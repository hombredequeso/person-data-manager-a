#!/bin/sh

echo Deleting person index...
curl -XDELETE -o /dev/null -s localhost:9200/person 
echo Creating person mapping...
curl -XPUT --data '@../../data/person-mapping.json' -H "Content-Type: application/json"  -o /dev/null -s localhost:9200/person

echo Adding data...
cat search-test-data.bulk | curl -XPOST localhost:9200/_bulk -o /dev/null --data-binary @-

echo Done.

