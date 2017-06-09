#!/bin/sh

echo Deleting person index...
curl -XDELETE -o /dev/null -s localhost:9200/person 
echo Creating person mapping...
curl -XPUT --data '@./person-mapping.json' -o /dev/null -s localhost:9200/person 

echo Adding data...
cd ../data-generator
node index.js --count 10000 | jq -c '.[] | {"index": {"_index": "person", "_type": "person", "_id": .id|tostring }}, .' | curl -XPOST localhost:9200/_bulk -o /dev/null --data-binary @-
cd ../data


echo Done.
