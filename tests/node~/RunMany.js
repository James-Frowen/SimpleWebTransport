const args = process.argv.slice(2);
const runCount = parseInt(args[0]);


const State = {
    NOT_STARTED: 'NOT_STARTED',
    RUNNING: 'RUNNING',
    STOPPED: 'STOPPED',
}

function RunMany(inner, timeoutBetweenRun = 20) {
    const currentlyRunning = [];
    for (let i = 0; i < runCount; i++) {
        currentlyRunning.push(State.NOT_STARTED);
        setTimeout(() => {
            currentlyRunning[i] = State.STARTED;

            function log(str) {
                console.log(`${i}: ${str}`);
            }
            function error(str) {
                console.error(`${i}: ${str}`);
            }
            function onExit() {
                currentlyRunning[i] = State.STOPPED;
                // check if all not running
                if (currentlyRunning.every(x => x == State.STOPPED)) {
                    process.exit(0);
                }
            }

            inner(onExit, log, error);
        }, i * timeoutBetweenRun);
    }

}
exports.RunMany = RunMany;