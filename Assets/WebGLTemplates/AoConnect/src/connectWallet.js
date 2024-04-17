import { fetchProcessTransactionsQuery } from "./graphqlQueries";

var registeredEvent = false;

export async function connectArweaveWallet()
{
    if (!globalThis.arweaveWallet) {
        alert('Error: No Arconnect extention installed!');
        return;
    }

    try {
        await globalThis.arweaveWallet.connect(['ACCESS_PUBLIC_KEY', 'SIGNATURE', 'ACCESS_ADDRESS', 'ACCESS_ALL_ADDRESSES', 'SIGN_TRANSACTION']);
    } catch (error) {
        console.error('Error connecting wallet:', error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');

        return null;
    }

    if (!registeredEvent)
    {
        addEventListener("walletSwitch", (e) => {
            const newAddress = e.detail.address;
            console.log("New Address: " + newAddress);
            myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', newAddress);
        });
        console.log("Registered");
        registeredEvent = true;
    }

    try
    {
        var activeAddress = await globalThis.arweaveWallet.getActiveAddress();
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', activeAddress);
    }
    catch (error)
    {
        console.error('Error checking active address:', error);
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateWallet', 'Error');
    }
}

export async function fetchProcesses (address) {
    const query = fetchProcessTransactionsQuery(address);
    const response = await fetch("https://arweave.net/graphql", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ query }),
    });

    console.log(response);
    if (response.ok) {
        const { data } = await response.json();
        const processes = data.transactions.edges.map((edge) => edge.node);
        console.log(`processes: `, processes);

        const processesString = JSON.stringify(processes);

        myUnityInstance.SendMessage('AOConnectManager', 'UpdateProcesses', processesString);
    }
    else
    {
        myUnityInstance.SendMessage('AOConnectManager', 'UpdateProcesses', "Error");
    }

};