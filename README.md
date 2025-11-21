# 🚀 Project Overview

This repository provides a complete, architecture-focused **learning template and reference implementation** for integrating modern .NET 8 applications with Keycloak.

The core principle of this guide is **strict HTTPS-only enforcement** in the local development environment. By avoiding insecure shortcuts like disabling `RequireHttpsMetadata` or using simple HTTP, this repository teaches the proper, production-aligned way to handle certificate trust, token validation, and session management.

This guide is designed for developers who are establishing a foundation in authentication. It covers the necessary theoretical background, a full hands-on setup, and advanced session management patterns that are often omitted in basic tutorials. 


# ⚠️ IMPORTANT: Learning & Template Focus

**This repository is intended solely for educational, development, and testing purposes.**

The infrastructure and code provided here should be treated as a blueprint for learning best practices. **It is NOT production-ready** and contains known simplifications (such as hardcoding the client secret in configuration for demonstration) that must be secured before moving to a real deployment.

# 🔑 Key Features

-   **🔒 HTTPS Everywhere:** Demonstrates how to create a local Certificate Authority (CA) using `mkcert` and configure it for full trust between the host machine, Keycloak (Docker), and.NET backchannel services.
    
-   **🔄 Robust Token Lifecycle:**
    
    -   **Decoupled Sessions:** Shows how to decouple the user's stable cookie session (60 min) from the short-lived access token (5 min) using `options.UseTokenLifetime = false`.
        
    -   **Automatic Refresh & Revocation:** Implements a robust `CustomCookieEvents` class to automatically renew expiring access tokens and check for session revocation by Keycloak .
        
    -   **DI-Managed Logic:** Refactors complex authentication logic out of `Program.cs` and into a Dependency Injection (DI) friendly class for maximum testability and maintainability.
        
-   **🛠️ Correct Configuration Patterns:** Provides the exact Keycloak realm and client settings, including the **Realm Role Mapper** necessary to correctly expose roles in a format that .NET's `` attribute understands .
    
-   **🎯 Architectural Clarity:** Explains the flow between the **Web App (OIDC Client)** and **Web API (JWT Resource Server)**, emphasizing backchannel communication requirements.

## Core Concepts (The Theory)

Before we build, here are the core concepts you need to know.

-   **Keycloak:** An open-source Identity and Access Management (IAM) server that acts as a central authority for all authentication.
    
-   **OpenID Connect (OIDC):** The standard protocol built on OAuth 2.0 used by the Web App to handle user login .
    
-   **JSON Web Token (JWT):** The digitally signed token used by the Web API to prove the identity and permissions of the caller .
    
-   **Access Token vs. Refresh Token:**
    
    -   **Access Token:** Short-lived (e.g., 5 minutes) and used to access protected resources (the Web API).
        
    -   **Refresh Token:** Long-lived and used only to obtain a new, fresh Access Token without requiring the user to re-authenticate .

## Setup Guide: From Zero to Secured

### Prerequisites

You will need the following tools installed:
    
1.  **Docker Desktop**
    
2.  **`mkcert`:** A simple tool for creating locally-trusted certificates.
    

### Step 1: Create a Locally-Trusted HTTPS Environment

This step ensures that all components trust each other without relying on insecure workarounds .

1.  **Install the Local CA:**
    
    Bash
    
    ```
    mkcert -install
    
    ```
    
    This creates a local Certificate Authority (CA) and installs it into your operating system's and browser's trust stores .
    
2.  **Generate the Certificate:** Create a secure directory (e.g., `C:\dev-certs`) and run the command to generate the certificate files for the custom hostname we'll use for Keycloak:
    
    Bash
    
    ```
    mkcert -cert-file./dev.crt -key-file./dev.key keycloak.dev.local localhost 127.0.0.1
    
    ```
    
    This creates `dev.crt` (certificate) and `dev.key` (private key) .
    
3.  **Edit Your `hosts` File:** Edit your system's `hosts` file to resolve the custom hostname to your local machine:
    
    ```
    127.0.0.1   keycloak.dev.local
    
    ```
    

### Step 2: Run Keycloak over HTTPS

We configure Keycloak to use the locally trusted certificate via `docker-compose`.

1.  **Create `docker-compose.yaml`:** The file mounts the certificates created in Step 1 and configures Keycloak to serve traffic over HTTPS on port 8443 .
    
    _(See the `docker-compose.yaml` file in the repository for the full content. Remember to update the `volumes` path.)_
    
2.  **Launch Keycloak:** Run `docker-compose up` in your terminal.
    
3.  **Verify:** Open `https://keycloak.dev.local:8443`. You should see the Keycloak admin console load **without any browser security warnings**, confirming the local trust is established.
    

### Step 3: Configure Keycloak Realm and Clients

Log in as `admin` / `admin` to perform the client configuration.

1.  **Create the Realm:** Create a new realm named `dotnet-realm`. (Avoid using the default `master` realm for application development) .
    
2.  **Create Clients:**
    
    -   **`dotnet-web-app` (OIDC Client):** Client Authentication `ON` (Confidential Client), Standard Flow `ON`. Set **Valid Redirect URIs** to `https://localhost:7001/signin-oidc`.
        
    -   **`dotnet-web-api` (Bearer-Only):** Client Authentication `OFF`, Standard Flow `OFF`. This client is purely for validating tokens on the backend.
        
3.  **Create Roles:** Create two Realm Roles: `admin` and `user` .
    
4.  **Create the Role Mapper (CRITICAL):**
    
    -   Go to **Client Scopes** -> `roles` -> **Mappers**.
        
    -   Add a mapper of type **User Realm Role**.
        
    -   Set **Token Claim Name:** `roles`.
        
    -   Set **Multivalued:** `On`.
        
    -   Ensure **Add to access token** is `On` .
        

This mapper is required because, by default, Keycloak structures roles in a way that is incompatible with the standard `.NET` authorization pipeline.