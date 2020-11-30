import threading, queue
import json
import requests
import uuid
import time
import random

q = queue.Queue(maxsize=1000)
avg_requests_per_second = 0
avg_requests_per_second_lock = threading.Lock()
NUM_CLIENTS = 20

def show_speed(*args, **kwargs):
    global avg_requests_per_second
    global calc_speed_timer
    global q
    lock = kwargs["lock"]
    lock.acquire()
    sum_ = avg_requests_per_second
    avg_requests_per_second = 0
    lock.release()
    print("{} REQUESTS PER SECOND. QUEUE SIZE[{}]".format(sum_ / 10.0, q.qsize()))
    calc_speed_timer = threading.Timer(10, show_speed, kwargs={"lock": lock})
    calc_speed_timer.start()

def client(*args, **kwargs):
    global avg_requests_per_second
    lock = kwargs["lock"]
    while True:
        item = q.get()
        try:
            response = requests.post(item['url'], data=json.dumps(item['body']), headers={'Content-Type': 'application/json', 'Accept': 'application/json', 'Accept-Encoding': 'gzip, deflate'})
            if response.status_code != 201:
                print(f"Error. Response: {response.content}")
            q.task_done()
        except requests.exceptions.ConnectionError as e:
            print(e)
        #print(f'Finished {item}. Response: {response.content}')

        lock.acquire()
        avg_requests_per_second += 1
        lock.release()
    
calc_speed_timer = threading.Timer(10, show_speed, kwargs={"lock": avg_requests_per_second_lock})
calc_speed_timer.start()

# turn-on the client threads
currencies = ["USD", "AUD", "BGN", "JPY", "GBP"]
clients = []
[clients.append(threading.Thread(target=client, daemon=True, kwargs={'lock': avg_requests_per_second_lock})) for _ in range(0, NUM_CLIENTS)]
[clients[_].start() for _ in range(0, NUM_CLIENTS)]

while True:
    t = time.time()
    time.localtime(t)
    gmtime = time.gmtime(t)
    requests_list = [
                ({'url': 'http://192.168.1.54:4000/json_api/current',
                  'body':
                    {
                     "requestId": str(uuid.uuid4()),
                     "timestamp": int(time.mktime(gmtime)),
                     "client": "1234",
                     "currency": random.choice(currencies)
                    }
                  }
                 ),
                ({'url':'http://192.168.1.54:4000/json_api/history',
                  'body':
                    {
                     "requestId": str(uuid.uuid4()),
                     "timestamp": int(time.mktime(gmtime)),
                     "client": "1234",
                     "currency": random.choice(currencies),
                     "period": int(random.random()*1000)+1500, 
                    }
                 }
                )
    ]
    q.put(random.choice(requests_list))

