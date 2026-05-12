import urllib.request, json
r = urllib.request.urlopen('http://localhost:8000/predict?date=2026-05-13')
data = json.loads(r.read())
print('=== Forecast for', data['date'], '===')
for f in data['forecast']:
    bar = '#' * (f['cycleTime'] // 5)
    print(f"  {f['hour']}  {f['status']:10s}  {f['cycleTime']:3d}s  {bar}")
