import threading, queue
import itertools
import json
import requests
import uuid
import time


q = queue.Queue(maxsize=1000)
avg_requests_per_second = 0
avg_requests_per_second_lock = threading.Lock()


def show_speed(*args, **kwargs):
    global avg_requests_per_second
    global calc_speed_timer
    lock = kwargs["lock"]
    lock.acquire()
    sum_ = avg_requests_per_second
    avg_requests_per_second = 0
    lock.release()
    print("{} REQUESTS PER SECOND".format(sum_ / 10.0))
    calc_speed_timer = threading.Timer(10, show_speed, kwargs={"lock": lock})
    calc_speed_timer.start()

def client(*args, **kwargs):
    global avg_requests_per_second
    lock = kwargs["lock"]
    while True:
        item = q.get()
        #print(f'Working on {item}')
        response = requests.post("http://localhost:4000/json_api/current", data=item, headers={'Content-Type': 'application/json', 'Accept': 'application/json', 'Accept-Encoding': 'gzip, deflate'})
        #print(f'Finished {item}. Response: {response.content}')
        q.task_done()
        lock.acquire()
        avg_requests_per_second += 1
        lock.release()
    
calc_speed_timer = threading.Timer(10, show_speed, kwargs={"lock": avg_requests_per_second_lock})
calc_speed_timer.start()

# turn-on the client threads
clients = []
[clients.append(threading.Thread(target=client, daemon=True, kwargs={'lock': avg_requests_per_second_lock})) for _ in range(0, 5)]
[clients[_].start() for _ in range(0, 5)]

for _ in itertools.count():
    payload = {
     "requestId": str(uuid.uuid4()),
     "timestamp": int(time.time()),
     "client": "1234",
     "currency": "USD"
    }
    q.put(json.dumps(payload))


clients[0].join()
print('All task requests sent\n', end='')
print('All work completed')
