const commandLineArgs = require('command-line-args');
const generator = require('./generator');

const optionDefinitions = [
  { name: 'count', alias: 'c', type: Number, defaultValue: 2 }
];
const option = commandLineArgs(optionDefinitions);

console.log = function(){};

let people = new Array(option.count)
                .fill(null)
                .map(_ => generator().next().value);

let serialized = JSON.stringify(people);
process.stdout.write(serialized);

