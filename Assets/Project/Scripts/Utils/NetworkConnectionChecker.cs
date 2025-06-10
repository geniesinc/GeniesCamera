using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;

public enum NetworkConnectionState
{
    Connected,
    NotConnected,
    Unknown
}

public class NetworkConnectionChecker : MonoBehaviour
{
    public delegate void ConnectionEvent(NetworkConnectionState state);
    public event ConnectionEvent NetworkConnectionStateChanged;

    // Note: Without https protocol, we will get the error of
    // "InvalidOperationException: Insecure connection not allowed"
    private string[] _testUrls = { "https://www.google.com/" };

    public NetworkConnectionState CurrentState { get; private set; } =
                        NetworkConnectionState.Unknown;

    private static readonly HttpClient _httpClient = new HttpClient();

    // We are using Start so that other objects can avoid a race condition and
    // subscribe to State Changed events during their Awake cycle.
    //
    async void Start()
    {
        // You've turned off cell and wifi
        if (Application.internetReachability == NetworkReachability.NotReachable &&
            CurrentState != NetworkConnectionState.NotConnected)
        {
            CurrentState = NetworkConnectionState.NotConnected;
            NetworkConnectionStateChanged?.Invoke(CurrentState);

            Debug.Log("CheckNetworkConnection: " + CurrentState);
            return;
        }
        // Other NetworkReachability status will tell you if the cell service is
        // on, or the WiFi is on-- but it won't tell you if those services are connected
        // to anything that is capable of transfering data.
        else
        {
            await CheckTestUrls();
        }
    }

    private async Task CheckTestUrls()
    {
        // prepare to check connections
        bool isSuccessful = false;

        for(int i=0; i < _testUrls.Length; i++)
        {
            try
            {
                HttpResponseMessage response = await _httpClient.GetAsync(_testUrls[i]);

                if (response.IsSuccessStatusCode)
                {
                    isSuccessful = true;
                    Debug.Log("Successfully verified internet connection.");
                }
                else
                {
                    Debug.LogError(_testUrls[i] + " pinged to test NetworkConnectivity. " +
                        "Received error: " + (int)response.StatusCode);
                }
            }
            catch (HttpRequestException e)
            {
                Debug.LogError(_testUrls[i] + " pinged to test NetworkConnectivity. " +
                        "Encountered HttpRequestException: " + e);
            }

            // break out of our URL testing loop
            if (isSuccessful)
            {
                break;
            }
        }

        // did our tracked state change?
        if ((isSuccessful) && (CurrentState != NetworkConnectionState.Connected))
        {
            CurrentState = NetworkConnectionState.Connected;
            NetworkConnectionStateChanged?.Invoke(CurrentState);
        }
        else if ((!isSuccessful) && (CurrentState != NetworkConnectionState.NotConnected))
        {
            CurrentState = NetworkConnectionState.NotConnected;
            NetworkConnectionStateChanged?.Invoke(CurrentState);
        }
    }
}