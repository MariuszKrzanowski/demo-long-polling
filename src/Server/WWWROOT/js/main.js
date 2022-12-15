/*
 
This file is based on jfsiii gist: showing how to download chunks messages by JavaScript
-  https://gist.github.com/jfsiii/034152ecfa908cf66178 

 */

window.addEventListener('load', ()=> {
    const placeholder = document.querySelector('[data-placeholder]');
    const chunkedUrlTemplate = '/events/next/gsn/';
    const lastSequenceNumber = /(.*["]globalSequenceNumber["]:)(\d+)([,]["]payload.*)/;
    
    const abortButton = document.querySelector('[data-abort]');
    const synchronizeButton = document.querySelector('[data-synchronize]');
    const cleanButton = document.querySelector('[data-clean]');
    const gsnInput = document.querySelector('[data-gsn]');
    gsnInput.text = '0';
        
    // window.lastSequenceNumber = lastSequenceNumber; // <== uncomment for debugging
    
    let abortController = new AbortController();
    
    cleanButton.addEventListener('click', ev => {
        placeholder.innerHTML = "";
    });
    
    abortButton.addEventListener('click', ev => {
        abortController.abort();
    });
    
    synchronizeButton.addEventListener('click', async ev => {
        await abortController.abort();
        abortController = new AbortController();
        const gsn = gsnInput.value
        fetch((chunkedUrlTemplate + gsn), {
            signal: abortController.signal
        })
            .then(processChunkedResponse)
            .then(onChunkedResponseComplete)
            .catch(onChunkedResponseError)
            ;
    });
    
    
    function onChunkedResponseComplete(result) {
        console.log('all done!', result)
    }
    
    function onChunkedResponseError(err) {
        console.error(err)
    }
    
    function processChunkedResponse(response) {
        var reader = response.body.getReader()
        var decoder = new TextDecoder();
    
        return readChunk();
    
        function readChunk() {
            return reader.read().then(appendChunks);
        }
    
        function appendChunks(result) {
            var chunk = decoder.decode(result.value || new Uint8Array, { stream: !result.done });
    
            const parts = chunk.split('\n');
            let lastGsn = gsnInput.value;
            parts.forEach(p => {
    
                if (p) {
                    var payloadElement=document.createElement('span');
                    payloadElement.classList.add('payload');
                    payloadElement.appendChild(document.createTextNode(p));

                    placeholder.appendChild(payloadElement);
                    //placeholder.appendChild(document.createElement('br'));
                    if (p.match(lastSequenceNumber)) {
                        lastGsn = (p.match(lastSequenceNumber))[2];
                    }
                }
            });
            gsnInput.value = lastGsn;
            placeholder.scrollTop = placeholder.scrollHeight - placeholder.clientHeight;
            // window.exposedChunk = chunk; // <== uncomment for debugging 
            console.log('got chunk of', chunk.length, 'bytes')
    
            if (result.done) {
                console.log('returning')
    
                placeholder.appendChild(document.createElement('hr'));
                return chunk
            } else {
                console.log('recursing')
                return readChunk();
            }
        }
    }
})
