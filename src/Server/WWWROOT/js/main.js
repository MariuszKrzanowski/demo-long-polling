/*

This file is based on jfsiii gist: showing how to download chunks messages by JavaScript
-  https://gist.github.com/jfsiii/034152ecfa908cf66178 

 */

window.addEventListener("load", () => {
  const placeholder = document.querySelector("[data-placeholder]");
  const chunkedUrlTemplate = "/events/next/gsn/";
  const appendRangeUrl = "/events/append-range";
  const lastSequenceNumber = /(.*["]gsn["]:)(\d+)([,]["]payload.*)/;

  const abortButton = document.querySelector("[data-abort]");
  const synchronizeButton = document.querySelector("[data-synchronize]");
  const resetGsnButton = document.querySelector("[data-reset-gsn]");
  const appendRangeButton = document.querySelector("[data-append-range]");
  const cleanButton = document.querySelector("[data-clean]");
  const gsnInput = document.querySelector("[data-gsn]");
  gsnInput.text = "0";

  // window.lastSequenceNumber = lastSequenceNumber; // <== uncomment for debugging

  var abortController = null;

  appendRangeButton.addEventListener("click", () => {
    console.log("manual command: append range");
    let arr = [];
    let i;
    for (i = 0; i < 1000; i++) {
      arr.push("chunk-" + i);
    }

    fetch(appendRangeUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify(arr),
    });
  });

  cleanButton.addEventListener("click", () => {
    placeholder.innerHTML = "";
    console.log("manual command: clean");
  });

  resetGsnButton.addEventListener("click", () => {
    gsnInput.value = "0";
  });

  abortButton.addEventListener("click", () => {
    abortController?.abort();
    abortController = null;
    console.log("manual command: abort");
  });

  synchronizeButton.addEventListener("click", () => {
    console.log("manual command: synchronize");
    abortController?.abort();
    const gsn = gsnInput.value;
    abortController = new AbortController();
    const signal = abortController.signal;
    const fetchUrl = chunkedUrlTemplate + gsn;

    fetch(fetchUrl, { signal })
      .then(processChunkedResponse)
      .then(onChunkedResponseComplete)
      .catch(onChunkedResponseError);
  });

  function onChunkedResponseComplete(result) {
    console.log("all done!", result);
  }

  function onChunkedResponseError(err) {
    console.error(err);
  }

  function processChunkedResponse(response) {
    var reader = response.body.getReader();
    var decoder = new TextDecoder();

    return readChunk();

    function readChunk() {
      return reader.read().then(appendChunks);
    }

    function appendChunks(result) {
      var chunk = decoder.decode(result.value || new Uint8Array(), {
        stream: !result.done,
      });

      const parts = chunk.split("\n");
      let lastGsn = gsnInput.value;
      parts.forEach((part) => {
        if (part) {
          var payloadElement = document.createElement("span");
          payloadElement.classList.add("payload");
          payloadElement.appendChild(document.createTextNode(part));

          placeholder.appendChild(payloadElement);
          if (part.match(lastSequenceNumber)) {
            lastGsn = part.match(lastSequenceNumber)[2];
          }
        }
      });
      gsnInput.value = lastGsn;
      placeholder.scrollTop =
        placeholder.scrollHeight - placeholder.clientHeight;
      // window.exposedChunk = chunk; // <== uncomment for debugging
      console.log("got chunk of", chunk.length, "bytes");

      if (result.done) {
        console.log("returning");
        
        return chunk;
      } else {
        console.log("recursing");
        return readChunk();
      }
    }
  }
});
