# 🚀 KubePulse

**KubePulse** is a .NET-based proof-of-concept that simulates web traffic to demonstrate Kubernetes autoscaling behavior using Minikube. It features:

* 🖥️ **ASP.NET Core Razor Pages frontend** to control request frequency
* 🧠 **ASP.NET Core Web API backend** to process requests and log heartbeats
* 🐘 **PostgreSQL database** to persist request logs
* ⚖️ **Kubernetes HPA (Horizontal Pod Autoscaler)** to scale backend based on CPU load
* ☸️ **Fully containerized** and deployed within **Minikube**

* * *

## 📁 Project Structure

    KubePulse/
    ├── CallerApp/         # Razor Pages frontend that generates requests
    ├── ProcessorApi/      # Minimal API backend that simulates processing delay
    ├── k8s/               # Kubernetes manifests (deployments, services, HPA)
    └── README.md
    

* * *

## 🚀 Getting Started

### ✅ Prerequisites

* [.NET 8 SDK](https://dotnet.microsoft.com/)
* [Minikube](https://minikube.sigs.k8s.io/)
* [Docker](https://www.docker.com/)
* [kubectl](https://kubernetes.io/docs/tasks/tools/) (optional)

* * *

### ⚙️ Step-by-Step Setup

#### 1\. Start Minikube

    minikube start
    minikube addons enable metrics-server
    

#### 2\. Build Docker Images Inside Minikube

    eval $(minikube docker-env)
    
    docker build -t kubepulse/caller-app:latest ./CallerApp
    docker build -t kubepulse/processor-api:latest ./ProcessorApi
    

#### 3\. Apply Kubernetes Resources

    kubectl apply -f k8s/postgres.yaml
    kubectl apply -f k8s/processor-api.yaml
    kubectl apply -f k8s/caller-app.yaml
    kubectl apply -f k8s/hpa.yaml
    

#### 4\. Access the UI

    minikube service caller-app
    

* * *

## 🧠 How It Works

* The **CallerApp** Razor Pages UI lets you set how often GET requests are sent.
* Requests hit the **ProcessorApi**, which:
  * Logs `StartTime` and `EndTime` in the database
  * Waits for a random delay (0–5 seconds) to simulate load
* Kubernetes HPA automatically scales the `processor-api` deployment based on CPU usage

* * *

## 🗃️ Database Schema

    CREATE TABLE RequestLogs (
        Id UUID PRIMARY KEY,
        StartTime TIMESTAMP NOT NULL,
        EndTime TIMESTAMP,
        SourceService TEXT
    );

Please note that this table is created by EF Migrations.

* * *

## 📈 Observability

    kubectl get hpa
    kubectl get pods -w
    

* * *

## 💡 Example Use Cases

* Simulate traffic spikes to test horizontal pod autoscaling
* Observe backend response under varying loads
* Learn Kubernetes deployment, autoscaling, and service orchestration with .NET

* * *

## 🛠 Tech Stack

* ASP.NET Core 8 (Razor Pages + Web API)
* PostgreSQL
* Docker
* Kubernetes + Minikube
* Horizontal Pod Autoscaler

* * *

## 📄 License

MIT License

* * *

## 🧠 Want to Learn Architecture Like This?

Built by [Software Architect GPT](https://sammuti.com) 🤖  
Need help designing scalable software systems? Let’s talk.
