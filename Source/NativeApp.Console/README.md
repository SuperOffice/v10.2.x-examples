# About

This project is a Native Console Application that uses the Authorization Code Flow to fetch an access_token.
It then demonstrates how to use this access_token towards both the REST API or use the NetServer Proxies.

## Setup

1. Look at appsettings-sample.json to see how the appsettings.json should be defined. This is the configuration for your application

    ```json
    {
        "ApplicationSettings": {
        "ClientId": "your-client-id",
        "ClientSecret": "your-client-secret",
        "Environment": "sod,stage,prod"
        }
    }
    ```

2. Make sure you have both WebApi and Services as available API Endpoints. The Server-to-server application has this by default: <https://docs.superoffice.com/en/developer-portal/create-app/server-to-server-app.html>

3. Add '^<http://127.0.0.1\:\d{4,10}$>' as Allowed redirect url: <https://docs.superoffice.com/en/developer-portal/create-app/config/cors-and-redirection-urls.html>

4. Select '.NET Core Launch (NativeApp) and Run (F5)
