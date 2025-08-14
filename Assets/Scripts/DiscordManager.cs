using System;
using UnityEngine;
using UnityEngine.UI;
using Discord.Sdk;

public class DiscordManager : MonoBehaviour
{
    [SerializeField] private ulong _clientId;
    [SerializeField] private Button _loginButton;
    [SerializeField] private Text _statusText;

    private Client _client;
    private string _codeVerifier;

    private void Start()
    {
        _client = new Client();

        // ログレベル(LoggingSeverity?)を調整する
        _client.AddLogCallback(OnLog, LoggingSeverity.Error);
        _client.SetStatusChangedCallback(OnStatusChanged);


        // コールバックの設定
        if (_loginButton != null)
        {
            _loginButton.onClick.AddListener(StartOAuthFlow);
        }
        else
        {
            Debug.LogError("ログインボタンの参照がないのでインスペクタから割り当ててください");
        }

        // ステータスのテキストを初期化
        if (_statusText != null)
        {
            _statusText.text = "ログイン準備完了";
        }
        else
        {
            Debug.LogError("ステータステキストの参照がないのでインスペクタから割り当ててください");
        }
    }

    private void OnLog(string msg, LoggingSeverity severity)
    {
        Debug.Log($"Log:{severity} - {msg}");
    }

    private void OnStatusChanged(Client.Status status, Client.Error error, int errorCode)
    {
        Debug.Log($"Status Changed: {status}");
        _statusText.text = status.ToString();
        if (error != Client.Error.None)
        {
            Debug.LogError($"Error: {error}, Code: {errorCode}");
        }

        if (status is Client.Status.Ready)
        {
            ClientReady();
        }
    }

    private void StartOAuthFlow()
    {
        var authVerifier = _client.CreateAuthorizationCodeVerifier();
        _codeVerifier = authVerifier.Verifier();

        var args = new AuthorizationArgs();
        args.SetClientId(_clientId);
        args.SetScopes(Client.GetDefaultPresenceScopes());
        args.SetCodeChallenge(authVerifier.Challenge());
        _client.Authorize(args, OnAuthorizeResult);
    }

    private void OnAuthorizeResult(ClientResult result, string code, string redirectUri)
    {
        Debug.Log($"Authorization Result: [{result.Error()}] [{code}] [{redirectUri}]");
        if (!result.Successful())
        {
            return;
        }

        GetTokenFromCode(code, redirectUri);
    }

    private void GetTokenFromCode(string code, string redirectUri)
    {
        _client.GetToken(_clientId, code, _codeVerifier, redirectUri,
            (result, token, refreshToken, type, @in, scopes) =>
            {
                if (token != String.Empty)
                {
                    OnReceivedToken(token);
                }
                else
                {
                    OnRetrieveCodeFailed();
                }
            });
    }

    private void OnReceivedToken(string token)
    {
        Debug.Log($"Received token: {token}");
        _client.UpdateToken(AuthorizationTokenType.Bearer, token, result => { _client.Connect(); });
    }

    private void OnRetrieveCodeFailed() => _statusText.text = "トークンの取得に失敗しました";

    private void ClientReady()
    {
        Debug.Log($"Friend Count: {_client.GetRelationships().Length}");

        Activity activity = new Activity();
        activity.SetType(ActivityTypes.Playing);
        activity.SetState("In Competitive Match");
        activity.SetDetails("Rank: Diamond II");
        _client.UpdateRichPresence(activity, (ClientResult result) =>
        {
            if (result.Successful())
            {
                Debug.Log("Rich presence updated!");
            }
            else
            {
                Debug.LogError("Failed to update rich presence");
            }
        });
    }
}