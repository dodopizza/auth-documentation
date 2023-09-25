# Get your hands dirty and implement Authorization Code Workflow easily with asp.net
This is `High Level` version.
This sample code uses build-in OIDC features of asp.net. It is designed to help to get started quickly if you choose dotnet and asp.net web application development stack.
If you want explore the details of OAuth2.0 and OIDC protocol as it works behind the scenes, you can check out an `Elbow Grease` version of the sample code.

## What's in this example
This example is a web server that presents minimal UI necessary to trigger OIDC authorization code workflow, get access to the protected resources under your client credentials that you've received from
Please refer to Get Access To The API section in this docs:
https://docs.dodois.io/

## Prerequisites
For building the dotnet Auth samples you need dotnet 7.0.102.
You can download and install it here:
https://dotnet.microsoft.com/en-us/download/dotnet/7.0

## Configuration
For the example to work, you need to provide your credentials for acessing the API.
Take a look at the 
[appsettings.template.json](src/AuthCode.Dotnet.HighLevel/appsettings.template.json)
Please copy the contents of this file to appsettings.json in the same directory (this file does not exist as you've just checked out the sample).
Edit the [appsettings.json](appsettings.json), filling with the credentials and properties you've received.
Never commit this file to Git. This file is in the .gitignore. If you choose to place the credentials in a file stored in the same directory in your real application you should also make sure this file is not committed to the repository.

## Development HTTPS certificate
For development with OAuth you need HTTPS even if you work from localhost.

Use the following command to check if you already have a valid trusted HTTPS certificate:

```shell
dotnet dev-certs https --check --trust
A valid HTTPS certificate is already present.
```

In my case I already have one. If you receive input telling that there is not certificate or it is not trusted, run the following command:

In case something goes wrong with this part, refer to the documentation here:
Use the [dotnet dev-certs](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-dev-certs) command to install and trust a valid self-signed development on your machine.

```sh
dotnet dev-certs https --trust
```

## Building

```shell
cd src
cd AuthCode.Dotnet.HighLevel
dotnet run
```

Now open the url https://localhost:5999 in the browser, replacing 5999 with watever you've configured as myAppUri. The port should be the same as in the redirectUri you requested in the API credentials form.

You are presented with a web interface, from which you can select the [Protected Resource](https://localhost:5999/Protected) link.

You should be redirected to the signin page where you enter your user credentials.
After signing in, you should get a consent screen:
![auth-documentation-consent-screen.png](../../docs/auth-documentation-consent-screen.png)

What is on the screen?

The consent screen includes information about the application and scopes of any non-public data you are giving consent for the app to access to.
In my case, it is `Auth gemba` app, which I am developing as I'm writing this code example. You will see your app name instead.
As for the scopes, I've requested `User identifier` and `Dodo Staff API` scopes, so I will be able to access user identifier and Dodo Staff API, which I will use for this code example later.

Also, you can see the `Works offline` item on the screen. It means I've requested a capability to get refresh tokens, so the app can refresh the access code without requiring the user to sign-in again after the initial code is expired.

After proceeding you should see the protected screen section of the website. It means you've successfully got the access and now you are authorized with OAuth2.0.
