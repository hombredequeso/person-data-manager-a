#!/bin/bash 
COUNTER=0
while [  $COUNTER -lt 50 ]; do

curl -X POST \

  http://localhost:8080/api/person/tag \
  -H 'content-type: application/json' \
  -d '{
	"addTag" : "itemNew11",
	"match": {
		"name" : {
			"firstName": "john",
			"lastName": ""
		},
		"tags": ["item3"]
	}
}'

    echo The counter is $COUNTER
    let COUNTER=COUNTER+1 
done
