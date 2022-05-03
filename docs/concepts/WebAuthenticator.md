## WebAuthenticator

The WebAuthenticator helps you simplify OAuth workflows in your application with a simple single-line call:

```cs
WebAuthenticatorResult result = await WinUIEx.WebAuthenticator.AuthenticateAsync(authorizeUrl, callbackUrl);
```

Your callback url must use a custom scheme, and you must define this scheme this in your application manifest:

![image](https://user-images.githubusercontent.com/1378165/166501267-1da07930-ab4d-431e-87cf-a7b183cc3c87.png)


