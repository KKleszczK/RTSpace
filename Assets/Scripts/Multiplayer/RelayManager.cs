using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Networking.Transport.Relay;
using UnityEngine;
using UnityEngine.UI;

public class RelayManager : MonoBehaviour
{

    
    private async Task InitUnityServices()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    public async Task<string> CreateRelay()
    {
        await InitUnityServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(1);

        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("Brak NetworkManager w scenie!");
            return null;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        if (transport == null)
        {
            Debug.LogError("Brak UnityTransport na obiekcie NetworkManager!");
            return null;
        }

        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        bool started = NetworkManager.Singleton.StartHost();

        if (!started)
        {
            Debug.LogError("Nie uda³o siê uruchomiæ hosta!");
            return null;
        }

        Debug.Log("JOIN CODE: " + joinCode);

        return joinCode;
    }

    public async Task JoinRelay(string joinCode)
    {
        await InitUnityServices();

        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetRelayServerData(new RelayServerData(allocation, "dtls"));

        NetworkManager.Singleton.StartClient();
    }
}