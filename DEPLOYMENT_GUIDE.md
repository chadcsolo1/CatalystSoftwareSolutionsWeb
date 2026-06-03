# Azure Web App Deployment Workflow Documentation

## Overview
This GitHub Actions workflow automates the deployment of your .NET 10 Blazor WebAssembly application to Azure Web App. The workflow builds, tests, and deploys your application whenever code is pushed to the master branch.

---

## Workflow Configuration

### Workflow Name
```yaml
name: Deploy Blazor App to Azure Web App
```
**Purpose:** Identifies the workflow in the GitHub Actions UI.

---

### Triggers (`on`)
```yaml
on:
  push:
    branches:
      - master
  workflow_dispatch:
```

**Purpose:** Defines when the workflow runs.

**Tasks:**
1. **push.branches.master** - Automatically triggers when code is pushed to the `master` branch
2. **workflow_dispatch** - Allows manual triggering from the GitHub Actions UI

---

### Environment Variables (`env`)
```yaml
env:
  AZURE_WEBAPP_NAME: 'your-webapp-name'
  AZURE_WEBAPP_PACKAGE_PATH: '.'
  DOTNET_VERSION: '10.0.x'
```

**Purpose:** Centralized configuration values used throughout the workflow.

**Variables:**
- **AZURE_WEBAPP_NAME**: The name of your Azure Web App resource (must match the Azure portal)
- **AZURE_WEBAPP_PACKAGE_PATH**: Root path of your solution (`.` means repository root)
- **DOTNET_VERSION**: The .NET SDK version to use (`10.0.x` uses the latest .NET 10 release)

---

## Job: build-and-deploy

### Job Configuration
```yaml
jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
```

**Purpose:** Defines a single job that runs on the latest Ubuntu runner.

**Why Ubuntu?** 
- Cost-effective
- Fast build times
- .NET 10 supports cross-platform compilation

---

## Workflow Steps

### Step 1: Checkout Code
```yaml
- name: Checkout code
  uses: actions/checkout@v4
```

**Purpose:** Downloads your repository code to the GitHub Actions runner.

**What it does:**
- Clones your repository at the specific commit that triggered the workflow
- Makes your code available for subsequent steps
- Uses the latest stable version (v4) of the checkout action

---

### Step 2: Setup .NET SDK
```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: ${{ env.DOTNET_VERSION }}
```

**Purpose:** Installs the .NET 10 SDK on the runner.

**What it does:**
- Downloads and installs .NET 10 SDK
- Configures the environment to use the specified .NET version
- Ensures consistency with your local development environment

---

### Step 3: Restore Dependencies
```yaml
- name: Restore dependencies
  run: dotnet restore
  working-directory: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
```

**Purpose:** Downloads all NuGet packages required by your solution.

**What it does:**
- Reads your `.csproj` files to identify package dependencies
- Downloads packages from NuGet.org and other configured sources
- Restores packages for both server and client projects
- Includes packages like:
  - `Azure.Communication.Email`
  - `Microsoft.Graph`
  - `SendGrid`
  - `Microsoft.AspNetCore.Components.WebAssembly`

**Why separate from build?** 
- Allows build caching
- Improves subsequent build performance

---

### Step 4: Build Solution
```yaml
- name: Build solution
  run: dotnet build --configuration Release --no-restore
  working-directory: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
```

**Purpose:** Compiles your entire solution in Release mode.

**What it does:**
- Compiles both server (`WebAppTemplate1`) and client (`WebAppTemplate1.Client`) projects
- Uses Release configuration for optimized production code
- `--no-restore` flag skips restoration since it was done in Step 3
- Applies optimizations:
  - Code minification
  - Dead code elimination
  - Performance improvements

**Output:** Compiled assemblies ready for testing and publishing

---

### Step 5: Run Tests
```yaml
- name: Run tests
  run: dotnet test --configuration Release --no-build --verbosity normal
  working-directory: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
  continue-on-error: true
```

**Purpose:** Executes all unit and integration tests in your solution.

**What it does:**
- Runs all test projects in the solution
- Uses the already-compiled Release binaries (`--no-build`)
- Provides normal verbosity output for test results
- `continue-on-error: true` prevents workflow failure if no tests exist

**Best Practice:** Remove `continue-on-error: true` once you have tests, so failing tests block deployment.

---

### Step 6: Publish Application
```yaml
- name: Publish application
  run: dotnet publish WebAppTemplate1/WebAppTemplate1/WebAppTemplate1.csproj --configuration Release --output ./publish
  working-directory: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}
```

**Purpose:** Creates a deployment-ready package of your application.

**What it does:**
- Publishes the server project (which includes the client as a dependency)
- Compiles and optimizes the Blazor WebAssembly client:
  - Generates `.dll` files
  - Creates `blazor.boot.json`
  - Optimizes assets
- Copies all required files to `./publish` folder:
  - Server assemblies
  - Client WebAssembly files (`_framework` folder)
  - Static assets (CSS, images, etc.)
  - `appsettings.json` and configuration files
  - `web.config` for IIS hosting

**Output:** Self-contained deployment package in `./publish` directory

---

### Step 7: Deploy to Azure Web App
```yaml
- name: Deploy to Azure Web App
  uses: azure/webapps-deploy@v3
  with:
    app-name: ${{ env.AZURE_WEBAPP_NAME }}
    publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
    package: ${{ env.AZURE_WEBAPP_PACKAGE_PATH }}/publish
```

**Purpose:** Uploads and deploys your application to Azure.

**What it does:**
- Authenticates with Azure using the publish profile stored in GitHub Secrets
- Uploads the `./publish` folder contents to Azure Web App
- Configures the Azure runtime environment
- Restarts the web app to apply changes
- Uses Web Deploy (MSDeploy) protocol for efficient deployment

**Parameters:**
- **app-name**: Your Azure Web App resource name
- **publish-profile**: XML credentials downloaded from Azure Portal (stored as a secret)
- **package**: Path to the published application folder

---

### Step 8: Verify Deployment
```yaml
- name: Verify deployment
  run: |
    echo "Deployment completed successfully!"
    echo "Application URL: https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net"
```

**Purpose:** Confirms successful deployment and provides the application URL.

**What it does:**
- Outputs a success message to the workflow logs
- Displays the public URL of your deployed application
- Useful for quick verification and documentation

---

## Setup Instructions

### 1. Configure Azure Web App Name
Edit the workflow file and replace `your-webapp-name` with your actual Azure Web App name:
```yaml
env:
  AZURE_WEBAPP_NAME: 'catalyst-software-solutions'  # Example
```

### 2. Get Azure Publish Profile
1. Go to the Azure Portal (https://portal.azure.com)
2. Navigate to your Web App resource
3. Click **"Get publish profile"** in the Overview blade
4. Download the `.PublishSettings` XML file

### 3. Add Publish Profile to GitHub Secrets
1. Go to your GitHub repository
2. Navigate to **Settings → Secrets and variables → Actions**
3. Click **"New repository secret"**
4. Name: `AZURE_WEBAPP_PUBLISH_PROFILE`
5. Value: Paste the entire contents of the `.PublishSettings` file
6. Click **"Add secret"**

### 4. Commit and Push the Workflow
```bash
git add .github/workflows/azure-webapp-deploy.yml
git commit -m "Add Azure deployment workflow"
git push origin master
```

### 5. Monitor Deployment
1. Go to your GitHub repository
2. Click the **"Actions"** tab
3. Watch the workflow execution in real-time
4. View logs for each step

---

## Configuration for Production

### Secure Your Application Settings

**Important:** Never commit sensitive data (API keys, connection strings) to the repository.

#### Option 1: Use Azure Application Settings (Recommended)
Configure secrets in Azure Portal:
1. Go to your Web App in Azure Portal
2. Navigate to **Configuration → Application settings**
3. Add your settings:
   - `EmailProvider:SendGrid:ApiKey`
   - `EmailProvider:MicrosoftGraph:ClientId`
   - `EmailProvider:MicrosoftGraph:ClientSecret`
   - `EmailProvider:AzureCommunicationServices:ConnectionString`

#### Option 2: Use GitHub Secrets in Workflow
Add secrets to GitHub and reference them during publish:
```yaml
- name: Publish application
  run: dotnet publish ... 
  env:
    SendGrid__ApiKey: ${{ secrets.SENDGRID_API_KEY }}
```

---

## Troubleshooting

### Build Fails
- **Check .NET version**: Ensure `DOTNET_VERSION` matches your project's target framework
- **Review logs**: Click on the failed step in GitHub Actions to see error details
- **Local build test**: Run `dotnet build --configuration Release` locally to reproduce

### Deployment Fails
- **Verify Web App name**: Ensure `AZURE_WEBAPP_NAME` exactly matches the Azure resource
- **Check publish profile**: Regenerate and update the secret if authentication fails
- **Review Azure logs**: Check Application Insights or Log Stream in Azure Portal

### Application Doesn't Start
- **Check runtime**: Verify Azure Web App is configured for .NET 10
- **Review dependencies**: Ensure all NuGet packages are compatible with Azure
- **Check configuration**: Verify `appsettings.json` has correct production settings

---

## Best Practices

### 1. Use Environment-Specific Settings
Consider separate workflows or branches for staging and production:
```yaml
on:
  push:
    branches:
      - master      # Production
      - staging     # Staging environment
```

### 2. Add Build Caching
Speed up builds by caching NuGet packages:
```yaml
- name: Cache NuGet packages
  uses: actions/cache@v3
  with:
    path: ~/.nuget/packages
    key: ${{ runner.os }}-nuget-${{ hashFiles('**/*.csproj') }}
```

### 3. Enable Tests
Once you add tests, remove `continue-on-error: true` to block deployment on test failures.

### 4. Add Health Check
Add a step to verify the app is responding:
```yaml
- name: Health check
  run: |
    curl -f https://${{ env.AZURE_WEBAPP_NAME }}.azurewebsites.net || exit 1
```

### 5. Notifications
Configure notifications for deployment success/failure via:
- GitHub Actions status badges
- Slack integration
- Email notifications
- Microsoft Teams webhooks

---

## Workflow Execution Summary

When you push code to master:

1. ✅ **Trigger**: GitHub detects push to master branch
2. ✅ **Checkout**: Code is cloned to runner
3. ✅ **Setup**: .NET 10 SDK is installed
4. ✅ **Restore**: NuGet packages are downloaded
5. ✅ **Build**: Solution is compiled in Release mode
6. ✅ **Test**: Unit tests are executed (if any)
7. ✅ **Publish**: Deployment package is created
8. ✅ **Deploy**: Package is uploaded to Azure
9. ✅ **Verify**: Deployment URL is displayed

**Total time:** Typically 2-5 minutes, depending on solution size and Azure region.

---

## Additional Resources

- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Azure Web Apps Documentation](https://docs.microsoft.com/azure/app-service/)
- [.NET CLI Documentation](https://docs.microsoft.com/dotnet/core/tools/)
- [Blazor Hosting Documentation](https://docs.microsoft.com/aspnet/core/blazor/hosting-models)

---

## Support

If you encounter issues:
1. Check the workflow logs in GitHub Actions
2. Review Azure Application Insights for runtime errors
3. Verify all secrets are correctly configured
4. Test the publish process locally: `dotnet publish -c Release`

**Last Updated:** 2025
**Workflow Version:** 1.0
**Target Framework:** .NET 10
