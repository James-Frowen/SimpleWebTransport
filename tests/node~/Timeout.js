setTimeout(() => {
    console.log("Should be running");
}, 1000);
setTimeout(() => {
    console.error("Should be stopped");
}, 10000);