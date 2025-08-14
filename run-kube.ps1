# Stop on error
$ErrorActionPreference = "Stop"

# --- Printing Current Date & Time ---
Write-Host "`n[+] Script Execution Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

# --- Starting a stop watch to meantoring the script execution time ---
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()

# --- Check if Minikube is running ---
Write-Host "`n[+] Checking if Minikube is running..."
$minikubeStatus = & minikube status --format "{{.Host}}"
if ($minikubeStatus -ne "Running") {
    Write-Host "`n[!] Minikube is not running. Starting Minikube..."
    & minikube start --driver=docker #--cpus=4 --memory=4096 --disk-size=20g these are wrong values. CPUs=2, Memory=3869MB these are currently running valid values
}
else {
    Write-Host "`n[+] Minikube is already running."
}

Write-Host "`n[+] Configuring Docker to use Minikube environment..."
& minikube -p minikube docker-env --shell=powershell | Invoke-Expression

# --- Enable Minikube Addons ---

# --- Check if Ingress addon is enabled ---
$ingressEnabled = & minikube addons list | Select-String "ingress"
if ($ingressEnabled -match "enabled") {
    Write-Host "`n[+] Ingress addon is already enabled."
}
else {
    Write-Host "`n[!] Ingress addon is not enabled. Enabling now..."
    Write-Host "`n[+] Enabling Ingress addon..."
    minikube addons enable ingress
}

# --- Check if Ingress-DNS addon is enabled ---
$ingressDnsEnabled = & minikube addons list | Select-String "ingress-dns"
if ($ingressDnsEnabled -match "enabled") {
    Write-Host "`n[+] Ingress-DNS addon is already enabled."
}
else {
    Write-Host "`n[!] Ingress-DNS addon is not enabled. Enabling now..."
    Write-Host "`n[+] Enabling Ingress-DNS addon..."
    minikube addons enable ingress-dns
}

# --- Check if Metrics Server addon is enabled ---
$metricsServerEnabled = & minikube addons list | Select-String "metrics-server"
if ($metricsServerEnabled -match "enabled") {
    Write-Host "`n[+] Metrics Server addon is already enabled."
}
else {
    Write-Host "`n[!] Metrics Server addon is not enabled. Enabling now..."
    Write-Host "`n[+] Enabling Metrics Server addon..."
    minikube addons enable metrics-server
}

# --- Build Docker Images ---
Write-Host "`n[+] Building Docker images for caller-app and processor-api..."
docker build -t kubepulse/caller-app:latest ./CallerApp
docker build -t kubepulse/processor-api:latest ./ProcessorApi

# --- Deploy Postgres + PVC ---
Write-Host "`n[+] Deploying Postgres and PVC..."
kubectl apply -f k8s/postgres.yaml

# --- Deploy pgAdmin ---
Write-Host "`n[+] Deploying pgAdmin..."
kubectl apply -f k8s/pgadmin.yaml # Not starting because we rarely use PgAdmin

# --- Deploy RabbitMQ ---
Write-Host "`n[+] Deploying RabbitMQ..."
kubectl apply -f k8s/rabbitmq.yaml

# --- Deploy Applications ---
Write-Host "`n[+] Deploying processor-api and caller-app..."
kubectl apply -f k8s/processor-api.yaml
kubectl apply -f k8s/caller-app.yaml

# --- Deploy HPA ---
Write-Host "`n[+] Deploying Horizontal Pod Autoscaler..."
kubectl apply -f k8s/hpa.yaml

# --- Apply Ingress Rules ---
# Write-Host "`n[+] Applying ingress rules..."
# kubectl apply -f k8s/ingress.yaml

# --- Restart Deployments ---
Write-Host "`n[+] Restarting all deployments..."
kubectl rollout restart deployment caller-app
kubectl rollout restart deployment processor-api
kubectl rollout restart deployment postgres
kubectl rollout restart deployment pgadmin # Not starting because we use PgAdmin
kubectl rollout restart deployment rabbitmq

# --- Wait for Pods ---
# Write-Host "`nWaiting for pods to be ready... (This might take a few minutes)"
# kubectl wait --for=condition=ready pod -l app=caller-app --timeout=120s
# kubectl wait --for=condition=ready pod -l app=processor-api --timeout=120s
# kubectl wait --for=condition=ready pod -l app=postgres --timeout=120s
# kubectl wait --for=condition=ready pod -l app=pgadmin --timeout=120s
# kubectl wait --for=condition=ready pod -l app=rabbitmq --timeout=120s

# --- Launch Dashboard and Apps (optional in background) ---

# --- Start Minikube dashboard ---
Write-Host "`n[+] Starting Minikube dashboard..."
Start-Job { minikube dashboard }

## --- Start Caller App ---
# Write-Host "`nStarting Caller App..."
# Start-Job { minikube service caller-app }

## --- Start pgAdmin ---
# Write-Host "`nStarting pgAdmin..."
# Start-Job { minikube service pgadmin }


Write-Host "`n[OK] All services deployed successfully!"
Write-Host ""
Write-Host ">>> IMPORTANT: RUN minikube tunnel <<<"
Write-Host "To access your applications, you must run the following command in a separate terminal with administrator privileges:"
Write-Host ""
Write-Host "  minikube tunnel"
Write-Host ""
Write-Host "Once 'minikube tunnel' is running, you can access your services via:"
Write-Host "  CallerApp      => http://localhost:9001/control"
Write-Host "  Processor API  => http://localhost:9002/process, http://localhost:9002/process2, http://localhost:9002/ProcessWithMessageQueue"
Write-Host "  pgAdmin        => http://localhost:9003/"
Write-Host "  Minikube Dashboard will open in browser automatically, port allocation is dynamic."

# --- Stop the stopwatch and display the elapsed time ---
$stopwatch.Stop()
Write-Host "`n[+] Script execution completed in $($stopwatch.Elapsed.TotalSeconds) seconds."

# --- Minikube Cleanup ---
# # Stop the cluster
# Write-Host "`n[+] Stopping Minikube..."
# & minikube stop

# # Delete all clusters
# Write-Host "`n[+] Deleting all Minikube clusters..."
# & minikube delete --all --purge