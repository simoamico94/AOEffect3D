import { createDataItemSigner, spawn } from "@permaweb/ao-sdk";

export async function spawnProcess(name) {

    const processId = await spawn({
    // The Arweave TXID of the ao Module
    module: "module TXID",
    // The Arweave wallet address of a Scheduler Unit
    scheduler: "TZ7o7SIZ06ZEJ14lXwVtng1EtSx60QkPy-kh-kdAXog",
    // A signer function containing your wallet
    signer: createDataItemSigner(globalThis.arweaveWallet),
    /*
        Refer to a Processes' source code or documentation
        for tags that may effect its computation.
    */
    tags: [
        { name: "Your-Tag-Name-Here", value: "your-tag-value" },
        { name: "Another-Tag", value: "another-value" },
    ],
    });

    console.log(processId);
}