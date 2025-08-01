# Stop on error
$ErrorActionPreference = "Stop"

Write-Host "`n[+] Configuring Docker to use Minikube environment..."
& minikube -p minikube docker-env --shell=powershell | Invoke-Expression

Write-Host "`n[+] Enabling Ingress addon..."
minikube addons enable ingress

Write-Host "`n[+] Enabling Metrics Server addon..."
minikube addons enable metrics-server

# --- Build Docker Images ---
Write-Host "`n[+] Building Docker images for caller-app and processor-api..."
docker build -t kubepulse/caller-app:latest ./CallerApp
docker build -t kubepulse/processor-api:latest ./ProcessorApi

# --- Deploy Postgres + PVC ---
Write-Host "`n[+] Deploying Postgres and PVC..."
kubectl apply -f k8s/postgres.yaml

# --- Deploy pgAdmin ---
Write-Host "`n[+] Deploying pgAdmin..."
kubectl apply -f k8s/pgadmin.yaml

# --- Deploy Applications ---
Write-Host "`n[+] Deploying processor-api and caller-app..."
kubectl apply -f k8s/processor-api.yaml
kubectl apply -f k8s/caller-app.yaml

# --- Deploy HPA ---
Write-Host "`n[+] Deploying Horizontal Pod Autoscaler..."
kubectl apply -f k8s/hpa.yaml

# --- Apply Ingress Rules ---
Write-Host "`n[+] Applying ingress rules..."
kubectl apply -f k8s/ingress.yaml

# --- Restart Deployments ---
Write-Host "`n[+] Restarting all deployments..."
kubectl rollout restart deployment caller-app
kubectl rollout restart deployment processor-api
kubectl rollout restart deployment postgres
kubectl rollout restart deployment pgadmin

# --- Wait for Pods ---
Write-Host "`nWaiting for pods to be ready... (This might take a few minutes)"
kubectl wait --for=condition=ready pod -l app=caller-app --timeout=120s
kubectl wait --for=condition=ready pod -l app=processor-api --timeout=120s
kubectl wait --for=condition=ready pod -l app=postgres --timeout=120s
kubectl wait --for=condition=ready pod -l app=pgadmin --timeout=120s

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
Write-Host "  CallerApp      => http://localhost:9001/caller"
Write-Host "  Processor API  => http://localhost:9002/processor"
Write-Host "  pgAdmin        => http://localhost:9003/pgadmin"
Write-Host "  Minikube Dashboard => Run: minikube dashboard"