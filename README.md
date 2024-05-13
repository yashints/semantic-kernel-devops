# Using semantic kernel in DevOps space

This repository demonstrates how to use [Microsoft's Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/overview/?tabs=Csharp) to generate pipelines for Azure DevOps or workflows for GitHub Actions in a neat way.

## How to run

To run the application first build the app:

```bash
dotnet build
```

And then run the app specifying the input file from the `desc` folder (you can add your own) and the function to be used by the kernel:

```bash
dotnet run -- -i .\desc\dotnet.txt -f GitHubActions
```

Above command invoked the `GitHubActions` function with the `dotnet` app as input. The result will look like below:

```yaml
FILE: .github/workflows/ci-cd.yml
#-----#
name: CI/CD Pipeline

on:
  push:
    branches:
      - main

jobs:
  build:
    name: Build
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v2

    - name: Setup .NET
      uses: actions/setup-dotnet@v1
      with:
        dotnet-version: '8.0.100'

    - name: Restore dependencies
      run: dotnet restore

    - name: Build All Projects
      run: dotnet build --no-restore

    - name: Run Tests
      run: dotnet test --no-build

  deploy:
    needs: build
    name: Deploy
    runs-on: ubuntu-latest
    environment:
      name: production

    steps:
    - name: Login to Azure
      uses: azure/login@v1
      with:
        creds: ${{ secrets.AZURE_CREDENTIALS }}

    - name: Set Azure Subscription
      uses: azure/powershell@v1
      with:
        inlineScript: Set-AzContext -SubscriptionId '${{ secrets.AZURE_SUBSCRIPTION_ID }}'

    - name: Deploy API Function
      uses: azure/functions-action@v1
      with:
        app-name: YourFunctionAppName
        package: './api'

    - name: Deploy Frontend App
      uses: azure/appservice-actions@v1
      with:
        app-name: YourAppName
        package: './frontend'

    - name: Logout of Azure
      uses: azure/logout@v1
#-----#

Please replace 'YourAppName', 'YourFunctionAppName' and 'subscriptions' with your appropriate Azure App Service Name, Function App and Subscription respectively.
```

## Settings

Do not forget to add your OpenAI or Azure OpenAI settings in the configuration file before running the application. You can use a local file which won't be committed to git like `appsettings.development.json` and set the environment variable `ASPNETCORE_ENVIRONMENT` to development.
