# run-kube.ps1

# Fail on error
$ErrorActionPreference = "Stop"

Write-Host "`nConfiguring Docker to use Minikube..."
& minikube -p minikube docker-env --shell=powershell | Invoke-Expression

# --- Build Images ---
Write-Host "`nBuilding Docker images for caller-app and processor-api..."
docker build -t kubepulse/caller-app:latest ./CallerApp
docker build -t kubepulse/processor-api:latest ./ProcessorApi

# --- Apply Postgres Config ---
Write-Host "`nDeploying Postgres and PVC..."
kubectl apply -f k8s/postgres.yaml

# --- Deploy pgAdmin ---
Write-Host "`nDeploying pgAdmin..."
kubectl apply -f k8s/pgadmin.yaml

# --- Apply Applications ---
Write-Host "`nDeploying processor-api and caller-app..."
kubectl apply -f k8s/processor-api.yaml
kubectl apply -f k8s/caller-app.yaml

# --- Apply HPA ---
Write-Host "`nDeploying Horizontal Pod Autoscaler..."
kubectl apply -f k8s/hpa.yaml

# --- Restart Services ---
Write-Host "`nRestarting Deployments..."
kubectl rollout restart deployment caller-app
kubectl rollout restart deployment processor-api
kubectl rollout restart deployment postgres
kubectl rollout restart deployment pgadmin

# --- Wait for Pods ---
Write-Host "`nWaiting for pods to be ready..."
kubectl wait --for=condition=ready pod -l app=caller-app --timeout=60s
kubectl wait --for=condition=ready pod -l app=processor-api --timeout=60s
kubectl wait --for=condition=ready pod -l app=postgres --timeout=60s
kubectl wait --for=condition=ready pod -l app=pgadmin --timeout=60s

Write-Host "`nAll services deployed successfully!"

# --- Start Minikube dashboard ---
Write-Host "`nStarting Minikube dashboard..."
Start-Job { minikube dashboard }

# --- Start Caller App ---
Write-Host "`nStarting Caller App..."
Start-Job { minikube service caller-app }

# --- Start pgAdmin ---
Write-Host "`nStarting pgAdmin..."
Start-Job { minikube service pgadmin }

# Informing user about application start
Write-Host "`nApplication is running. You can access it via the Minikube service URLs."
