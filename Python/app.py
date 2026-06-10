from os import listdir, rename, getenv
from dotenv import load_dotenv
from mssql_python import connect
from azure.identity import DefaultAzureCredential
from azure.storage.blob import BlobServiceClient
import csv
import logging
import uuid
import json

load_dotenv()

# Set Debug Level
DEBUG = getenv("DEBUG")
if (DEBUG.upper() == "TRUE"):
    logging.basicConfig(level=logging.DEBUG)
else:
    logging.basicConfig(level=logging.INFO)


blob_account_url = getenv("BLOB_URL")

# Create the BlobServiceClient object
default_credential = DefaultAzureCredential()
blob_service_client = BlobServiceClient(blob_account_url, credential=default_credential)

def get_conn():
    """Connect using mssql-python with built-in Microsoft Entra authentication."""
    connection_string = getenv("SQL_CONNECTION_STRING")
    conn = connect(connection_string)
    conn.setautocommit(True)
    return conn

def update_race_with_communique_id(race_id: int, communique_type: str, communique_file_name: str):
    if communique_type not in ("START", "FINISH"):
        return None
    if communique_type == "START":
        column_name = "StartCommuniqueId"
    elif communique_type == "FINISH":
        column_name = "ResultCommuniqueId"
    
    with get_conn() as conn:
        cursor = conn.cursor()
        query = """
           UPDATE Race
           SET {columnName} = '{file_name}'
           WHERE id = {race_id}
        """.format(columnName=column_name, file_name=communique_file_name, race_id=race_id)
        logging.info("SQL Query = " + query)
        cursor.execute(query)
        conn.commit()
        logging.info(f'{cursor.rowcount} rows updated successfully.')
        if cursor.rowcount == 1:
            return True
        else:
            return False

def upload_blob_data(blob_service_client: BlobServiceClient, blob_name: str, blob_contents: object):
    blob_client = blob_service_client.get_blob_client(container="communiques", blob=blob_name)
    data = blob_contents
    # Upload the blob data - default blob type is BlockBlob
    blob_client.upload_blob(data, blob_type="BlockBlob")

def process_start_csv(f):
    with open('start\\'+f, 'r') as f:
        r = csv.reader(f)

        headers = []
        heats = []
        js = {}
        temp = {}
        riderList = []
        
        HEAD = True
        FIRST = True

        for row in r:
            logging.debug(row)
            if (row[0] == '' and row[1] == ''):
                logging.debug("Section Break")
                HEAD = False
                if (not FIRST):
                    logging.info("Adding heat of %s riders" % len(riderList))
                    if(len(riderList) > 0):
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
                        logging.debug("Adding rider %s" % row[1])
                        riderList.append(rider)
        
        
        if(len(riderList) > 0):
            logging.info("Adding heat of %s riders" % len(riderList))
            temp['Riders'] = riderList
            heats.append(temp)
    js['Start'] = heats
            
    return js


def process_finish_csv(f):
    with open('finish\\'+f, 'r') as f:
        r = csv.reader(f)

        headers = []
        heats = []
        js = {}
        temp = {}
        riderList = []
        
        HEAD = True
        FIRST = True

        for row in r:
            logging.debug(row)
            if (row[0] == '' and row[1] == ''):
                logging.debug("Section Break")
                HEAD = False
                if (not FIRST):
                    logging.info("Adding heat of %s riders" % len(riderList))
                    if(len(riderList) > 0):
                        temp['RiderResults'] = riderList
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
                    if (row[1].upper() == 'RANK'):
                        heatName = row[0]
                        temp['HeatTitle'] = heatName
                    else:
                        rider = {
                            'Rank': row[1],
                            'Bib': row[2],
                            'Name' : row[3],
                            'Nation' : row[4],
                            'ResultDetails' : row[5]
                        }
                        logging.debug("Adding rider %s" % row[1])
                        riderList.append(rider)
        
        
        if(len(riderList) > 0):
            logging.info("Adding heat of %s riders" % len(riderList))
            temp['RiderResults'] = riderList
            heats.append(temp)
    js['Result'] = heats
            
    return js

start_files = listdir('./start')
finish_files = listdir('./finish')

for start_file in start_files:
    race_id = start_file.split('.')[0]
    file_contents = process_start_csv(start_file)
    file_contents = json.dumps(file_contents)
    communique_file_name = str(uuid.uuid4()) + '.json'
    with open('output\\' + communique_file_name, 'w') as f:
        f.write(file_contents)
    logging.info("Uploading %s: " % communique_file_name)
    upload_blob_data(blob_service_client, communique_file_name, file_contents)
    update_race_with_communique_id(race_id, "START", communique_file_name)
    rename("start\\" + start_file, "processed\\start\\" + start_file)

for finish_file in finish_files:
    race_id = finish_file.split('.')[0]
    file_contents = process_finish_csv(finish_file)
    file_contents = json.dumps(file_contents)
    communique_file_name = str(uuid.uuid4()) + '.json'
    with open('output\\' + communique_file_name, 'w') as f:
        f.write(file_contents)
    # logging.info("Uploading %s: " % communique_file_name)
    # upload_blob_data(blob_service_client, communique_file_name, file_contents)
    # success = update_race_with_communique_id(race_id, "FINISH", communique_file_name)
    # if (success):
    #     rename("finish\\" + finish_file, "processed\\finish\\" + finish_file)