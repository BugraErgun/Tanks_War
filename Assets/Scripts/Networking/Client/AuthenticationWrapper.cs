using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public static class AuthenticationWrapper 
{
    public static AuthState AuthState { get; private set; } = AuthState.NotAuthenticated;

    public static async Task<AuthState> DoAuth(int maxRetries = 5)
    {
        if(AuthState == AuthState.Authenticated)
        {
            return AuthState;
        }

        if(AuthState == AuthState.Authentucating)
        {
            Debug.LogWarning("Already authenticating!");
            await Authenticating();
            return AuthState;
        }

        await SignInAnonymouslyAsync(maxRetries);

        return AuthState;
    }

    private static async Task<AuthState> Authenticating()
    {
        while (AuthState == AuthState.Authentucating || AuthState == AuthState.NotAuthenticated)
        {
            await Task.Delay(200);
        }
        return AuthState;
    }

    private static async Task SignInAnonymouslyAsync(int maxRetries)
    {
        AuthState = AuthState.Authentucating;

        int retries = 0;

        while (AuthState == AuthState.Authentucating && retries < maxRetries)
        {
            try
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

                if (AuthenticationService.Instance.IsSignedIn && AuthenticationService.Instance.IsAuthorized)
                {
                    AuthState = AuthState.Authenticated;
                    break;
                }
            }
            catch(AuthenticationException authException)
            {
                Debug.Log(authException);
                AuthState = AuthState.Error;
            }
            catch(RequestFailedException requestException)
            {
                Debug.Log(requestException);  
                AuthState = AuthState.Error;
            }

            retries++;
            await Task.Delay(1000);
        }
        if(AuthState != AuthState.Authenticated)
        {
            Debug.LogWarning($"Player was not signed in succesfully after {retries} tries");
            AuthState = AuthState.TimeOut;
        }
    }
}

public enum AuthState
{
    NotAuthenticated,
    Authentucating,
    Authenticated,
    Error,
    TimeOut
}