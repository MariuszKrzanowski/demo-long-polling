/*
  This file is based on jfsiii gist: showing how to download chunks messages by JavaScript
  https://gist.github.com/jfsiii/034152ecfa908cf66178 
*/

const chunkedUrlTemplate = "/Events/next/gsn/";
const appendRangeUrl = "/Events/append-range";
const lastSequenceNumber = /(.*["]gsn["]:)(\d+)([,]["]payload.*)/;

window.addEventListener("load", () => {
  let longPollingAbortController = new AbortController();

  const mainForm = document.querySelector("form");
  const placeholder = document.querySelector("[data-placeholder]");
  const abortButton = document.querySelector("[data-abort]");
  const synchronizeButton = document.querySelector("[data-synchronize]");
  const resetGsnButton = document.querySelector("[data-reset-gsn]");
  const appendRangeButton = document.querySelector("[data-append-range]");
  const cleanButton = document.querySelector("[data-clean]");
  const gsnInput = document.querySelector("[data-gsn]");
  gsnInput.value = "0";

  mainForm.addEventListener("submit", () => {
    return false;
  });

  // window.lastSequenceNumber = lastSequenceNumber; // <== uncomment for debugging

  appendRangeButton.addEventListener("click", () => {
    console.log("manual command: append range");
    const appendChunks = [];
    let idx = 0;
    for (idx = 0; idx < 5000; idx++) {
      appendChunks.push("chunk-" + idx);
    }

    const jsonBody = JSON.stringify(appendChunks);
    console.log(jsonBody);

    fetch(appendRangeUrl, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        accept: "*/*",
      },
      body: jsonBody,
    })
      .then((r) => {
        console.log("then 1");
      })
      .then((r) => {
        console.log("then 2");
      })
      .catch((err) => {
        console.error(err);
        alert(err);
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
    longPollingAbortController.abort();
    console.log("manual command: abort");
  });

  synchronizeButton.addEventListener("click", () => {
    console.log("manual command: synchronize");
    longPollingAbortController.abort();
    const gsn = gsnInput.value;
    longPollingAbortController = new AbortController();
    const signal = longPollingAbortController.signal;
    const fetchUrl = chunkedUrlTemplate + gsn;

    fetch(fetchUrl, {
      signal: signal,
      headers: {
        "Content-Type": "application/json",
        accept: "application/x-ndjson",
      },
    })
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
