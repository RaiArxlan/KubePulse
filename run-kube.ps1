# run-kube.ps1

Write-Host "Configuring Docker to use Minikube..."
& minikube -p minikube docker-env --shell=powershell | Invoke-Expression

# --- Build Images ---
Write-Host "Building Docker images for caller-app and processor-api..."
docker build -t kubepulse/caller-app:latest ./CallerApp
docker build -t kubepulse/processor-api:latest ./ProcessorApi

# --- Apply Postgres Config ---
Write-Host "Deploying Postgres and PVC..."
kubectl apply -f k8s/postgres.yaml

# --- Apply Applications ---
Write-Host "Deploying processor-api and caller-app..."
kubectl apply -f k8s/processor-api.yaml
kubectl apply -f k8s/caller-app.yaml

# --- Apply HPA ---
Write-Host "Deploying Horizontal Pod Autoscaler..."
kubectl apply -f k8s/hpa.yaml

# --- Restart Services ---
Write-Host "Restarting Deployments..."
kubectl rollout restart deployment caller-app
kubectl rollout restart deployment processor-api
kubectl rollout restart deployment postgres

# --- Wait for Pods ---
Write-Host "Waiting for pods to be ready..."
kubectl wait --for=condition=ready pod -l app=caller-app --timeout=60s
kubectl wait --for=condition=ready pod -l app=processor-api --timeout=60s
kubectl wait --for=condition=ready pod -l app=postgres --timeout=60s

# --- Print Access Info ---
$minikubeIp = minikube ip
$callerUrl = "http://$minikubeIp:30080"
$processorUrl = "http://$minikubeIp:30081/process"

Write-Host "All services deployed successfully!"
Write-Host "Caller UI:      $callerUrl"
Write-Host "Processor API:  $processorUrl"

# --- Optionally Open Browser ---
Start-Process $callerUrl
