import os
import csv
import uuid
import json

print(os.getcwd())

with open('SLGP\\W-Omnium-Points-Start.csv', 'r') as f:
    r = csv.reader(f)

    headers = []
    HEAD = True
    FIRST = True
    heats = []
    js = {}
    temp = {}
    riderList = []
    
    for row in r:
        
        print(row)
        if (row[0] == '' and row[1] == ''):
            print("Section Break")
            HEAD = False
            if (not FIRST):
                temp['Riders'] = riderList
                heats.append(temp)
                temp = {}
                riderList = []
            FIRST = False
            pass
        else:
            if (HEAD):
                js[row[0]] = row[1]
                if (row[0] == 'CommuniqueType'):
                    js[row[0]] = int(row[1])
            else:
                if (row[0] != ''):
                    heatName = row[0]
                    temp['HeatTitle'] = heatName
                else:
                    rider = {
                        'Bib': row[1],
                        'Name': row[2],
                        'Nation' : row[3]
                    }
                    print("Adding rider %s" % row[1])
                    riderList.append(rider)
    
    temp['Riders'] = riderList
    heats.append(temp)

js['Start'] = heats
        
print(json)
communiqueId = str(uuid.uuid4())

with open (communiqueId+'.json', 'w') as f:
    json.dump(js, f)