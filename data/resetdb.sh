#!/bin/sh

echo Deleting person index...
curl -XDELETE -o /dev/null -s localhost:9200/person 
echo Creating person mapping...
curl -XPUT --data '@./person-mapping.json' -o /dev/null -s localhost:9200/person 

echo Adding data...
node ../../person-random-data-generator/util/create-people.js --generator ./person-data-manager-generator-1.js --count 10000 | jq -c '.[] | {"index": {"_index": "person", "_type": "person"}}, .' | curl -XPOST localhost:9200/_bulk -o /dev/null --data-binary @-

echo Done.
