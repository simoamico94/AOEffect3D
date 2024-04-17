import { createDataItemSigner, spawn } from "@permaweb/aoconnect";

export async function spawnProcess(name)
{
    const processId = await spawn({
    // The Arweave TXID of the ao Module
    module: "9afQ1PLf2mrshqCTZEzzJTR2gWaC9zNPnYgYEqg1Pt4",
    // The Arweave wallet address of a Scheduler Unit
    scheduler: "_GQ33BkPtZrqxA84vM8Zk-N2aO0toNNu_C-l-rawrBA",
    // A signer function containing your wallet
    signer: createDataItemSigner(globalThis.arweaveWallet),
    /*
        Refer to a Processes' source code or documentation
        for tags that may effect its computation.
    */
    tags: [
        { name: "Name", value: name },
        //{ name: "Another-Tag", value: "another-value" },
    ],
    });

    console.log(processId);
    myUnityInstance.SendMessage('AOConnectManager', 'SpawnProcessCallback', processId);
}