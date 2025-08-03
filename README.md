# 🚀 KubePulse

**KubePulse** is a .NET-based proof-of-concept that simulates web traffic to demonstrate Kubernetes autoscaling behavior using Minikube. It features:

* 🖥️ **ASP.NET Core Razor Pages frontend** to control request frequency
* 🧠 **ASP.NET Core Web API backend** to process requests and log heartbeats
* 🐘 **PostgreSQL database** to persist request logs
* ⚖️ **Kubernetes HPA (Horizontal Pod Autoscaler)** to scale backend based on CPU load
* ☸️ **Fully containerized** and deployed within **Minikube**

* * *

## 📁 Project Structure

```text
KubePulse/
├── CallerApp/         # Razor Pages frontend that generates requests
├── ProcessorApi/      # Minimal API backend that simulates processing delay
├── k8s/               # Kubernetes manifests (deployments, services, HPA)
└── README.md
```

* * *

## 🚀 Getting Started

### ✅ Prerequisites

* [.NET 9 SDK](https://dotnet.microsoft.com/)
* [Minikube](https://minikube.sigs.k8s.io/)
* [Docker](https://www.docker.com/)
* [kubectl](https://kubernetes.io/docs/tasks/tools/) (optional)

* * *

### ⚙️ Step-by-Step Setup

#### 1\. Start Minikube

```powershell
minikube start --driver=docker
minikube addons enable metrics-server
minikube addons enable ingress
```

#### 2\. Build Docker Images Inside Minikube

```powershell
& minikube -p minikube docker-env --shell=powershell | Invoke-Expression

docker build -t kubepulse/caller-app:latest ./CallerApp
docker build -t kubepulse/processor-api:latest ./ProcessorApi
```

#### 3\. Deploy All Resources (Automated)

Run the provided PowerShell script to automate deployment:

```powershell
.\run-kube.ps1
```

This script will:

* Configure Docker to use Minikube
* Enable required Minikube addons
* Build Docker images
* Apply all Kubernetes manifests (Postgres, pgAdmin, apps, HPA, Ingress)
* Restart deployments and wait for pods to be ready
* Start the Minikube dashboard in the background

#### 4\. Access Your Applications

* Run `minikube tunnel` in a separate terminal (with admin privileges) to expose LoadBalancer services.
* Once the tunnel is running, access your services at:
  * CallerApp: [http://localhost:9001/caller](http://localhost:9001/caller)
  * Processor API: [http://localhost:9002/processor](http://localhost:9002/processor)
  * pgAdmin: [http://localhost:9003/pgadmin](http://localhost:9003/pgadmin)
  * Minikube Dashboard: Run `minikube dashboard`

* * *

## 🧠 How It Works

* The **CallerApp** Razor Pages UI lets you set how often GET requests are sent.
* Requests hit the **ProcessorApi**, which:
  * Logs `StartTime` and `EndTime` in the database
  * Waits for a random delay (0–5 seconds) to simulate load
* Kubernetes HPA automatically scales the `processor-api` deployment based on CPU and memory usage.

* * *

## 🗃️ Database Schema

```SQL
CREATE TABLE "RequestLogs" (
    "Id" UUID PRIMARY KEY,
    "StartTime" TIMESTAMP NOT NULL,
    "EndTime" TIMESTAMP,
    "SourceService" TEXT
);
```

Please note that this table is created by EF Migrations.

* * *

## 📈 Observability

```powershell
kubectl get hpa
kubectl get pods -w
```

* * *

## 💡 Example Use Cases

* Simulate traffic spikes to test horizontal pod autoscaling
* Observe backend response under varying loads
* Learn Kubernetes deployment, autoscaling, and service orchestration with .NET

* * *

## 🛠 Tech Stack

* ASP.NET Core 9 (Razor Pages + Web API)
* PostgreSQL
* Docker
* Kubernetes + Minikube
* Horizontal Pod Autoscaler

* * *

## 👤 About the Author

**Rai Arslan**  
Full-Stack Software Engineer | 6+ years experience

* 💡 Passionate about open source, ERP, CMS, and freelance projects
* 📬 Reach out via [email](mailto:raiarxlan@gmail.com)
* 💼 Connect on [LinkedIn](https://www.linkedin.com/in/raiarxlan/)
* 💻 Explore my work on [GitHub](https://github.com/RaiArxlan/)

Feel free to connect or collaborate!
