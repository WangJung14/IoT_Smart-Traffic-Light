import requests
import json
import time

URL = "http://localhost:5212/api/v1/hardware/vehicle-counts"

def test_webster_api():
    print("Testing Webster API with manual vehicle counts...")
    
    # Mock data: Heavy traffic on NS, Low on EW
    payload = {
        "nsVehicles": {"car": 10, "motorbike": 20, "bus": 2, "truck": 1},
        "ewVehicles": {"car": 2, "motorbike": 5, "bus": 0, "truck": 0}
    }
    
    try:
        response = requests.post(URL, json=payload)
        print(f"Status Code: {response.status_code}")
        if response.status_code == 200:
            print("Response Data:")
            print(json.dumps(response.json(), indent=2))
        else:
            print(f"Error: {response.text}")
    except Exception as e:
        print(f"Connection failed: {e}")

if __name__ == "__main__":
    time.sleep(10) # Wait for server to start
    test_webster_api()
