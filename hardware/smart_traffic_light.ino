unsigned int RED_DURATION    = 50; // Used for EW Green
unsigned int GREEN_DURATION  = 35; // Used for NS Green
const unsigned int YELLOW_DURATION =  5;

// Chân đèn: (Đỏ, Vàng, Xanh)
const int N_RED=2,  N_YELLOW=3,  N_GREEN=4;   // Bắc
const int E_RED=5,  E_YELLOW=6,  E_GREEN=7;   // Đông
const int S_RED=8,  S_YELLOW=9,  S_GREEN=10;  // Nam
const int W_RED=11, W_YELLOW=12, W_GREEN=13;  // Tây

enum TrafficState {
  NS_GREEN_EW_RED,    // B-N xanh, Đ-T đỏ
  NS_YELLOW_EW_RED,   // B-N vàng, Đ-T đỏ
  NS_RED_EW_GREEN,    // B-N đỏ,   Đ-T xanh
  NS_RED_EW_YELLOW    // B-N đỏ,   Đ-T vàng
};

TrafficState currentState;
unsigned long stateStartTime, stateDuration;

void setup() {
  Serial.begin(9600);
  int pins[] = {N_RED,N_YELLOW,N_GREEN, E_RED,E_YELLOW,E_GREEN,
                S_RED,S_YELLOW,S_GREEN, W_RED,W_YELLOW,W_GREEN};
  for (int i = 0; i < 12; i++) { pinMode(pins[i], OUTPUT); digitalWrite(pins[i], LOW); }

  enterState(NS_GREEN_EW_RED);
  Serial.println(F("=== DEN GIAO THONG 4 HUONG ==="));
  Serial.print(F("Do:")); Serial.print(RED_DURATION);
  Serial.print(F("s Xanh:")); Serial.print(GREEN_DURATION);
  Serial.print(F("s Vang:")); Serial.print(YELLOW_DURATION);
  Serial.print(F("s | Chu ky:")); Serial.print(RED_DURATION+GREEN_DURATION+YELLOW_DURATION*2);
  Serial.println(F("s | S=Status R=Reset"));
}

void loop() {
  handleSerial();
  if (millis() - stateStartTime >= stateDuration) nextState();
}

void nextState() {
  switch (currentState) {
    case NS_GREEN_EW_RED:  enterState(NS_YELLOW_EW_RED); break;
    case NS_YELLOW_EW_RED: enterState(NS_RED_EW_GREEN);  break;
    case NS_RED_EW_GREEN:  enterState(NS_RED_EW_YELLOW); break;
    case NS_RED_EW_YELLOW: enterState(NS_GREEN_EW_RED);  break;
  }
}

void enterState(TrafficState s) {
  currentState = s; stateStartTime = millis();
  allOff();
  switch (s) {
    case NS_GREEN_EW_RED:
      on2(N_GREEN,S_GREEN); on2(E_RED,W_RED);
      stateDuration = (unsigned long)GREEN_DURATION * 1000UL;
      Serial.print(F("[B-N:XANH D-T:DO] ")); Serial.print(GREEN_DURATION); Serial.println(F("s"));
      break;
    case NS_YELLOW_EW_RED:
      on2(N_YELLOW,S_YELLOW); on2(E_RED,W_RED);
      stateDuration = (unsigned long)YELLOW_DURATION * 1000UL;
      Serial.print(F("[B-N:VANG D-T:DO] ")); Serial.print(YELLOW_DURATION); Serial.println(F("s"));
      break;
    case NS_RED_EW_GREEN:
      on2(N_RED,S_RED); on2(E_GREEN,W_GREEN);
      stateDuration = (unsigned long)RED_DURATION * 1000UL;
      Serial.print(F("[B-N:DO D-T:XANH] ")); Serial.print(RED_DURATION); Serial.println(F("s"));
      break;
    case NS_RED_EW_YELLOW:
      on2(N_RED,S_RED); on2(E_YELLOW,W_YELLOW);
      stateDuration = (unsigned long)YELLOW_DURATION * 1000UL;
      Serial.print(F("[B-N:DO D-T:VANG] ")); Serial.print(YELLOW_DURATION); Serial.println(F("s"));
      break;
  }
}

void allOff() {
  int pins[] = {N_RED,N_YELLOW,N_GREEN, E_RED,E_YELLOW,E_GREEN,
                S_RED,S_YELLOW,S_GREEN, W_RED,W_YELLOW,W_GREEN};
  for (int i = 0; i < 12; i++) digitalWrite(pins[i], LOW);
}

void on2(int p1, int p2) { digitalWrite(p1,HIGH); digitalWrite(p2,HIGH); }

void handleSerial() {
  if (!Serial.available()) return;
  String input = Serial.readStringUntil('\n');
  input.trim();
  if (input.length() == 0) return;

  if (input.equalsIgnoreCase("S")) {
    printStatus();
  } else if (input.equalsIgnoreCase("R")) {
    Serial.println(F(">> Reset...")); 
    enterState(NS_GREEN_EW_RED);
  } else if (input.startsWith("T:")) {
    // Format: T:ns_green,ew_green
    int commaIndex = input.indexOf(',');
    if (commaIndex > 2) {
      int newNs = input.substring(2, commaIndex).toInt();
      int newEw = input.substring(commaIndex + 1).toInt();
      if (newNs > 0 && newEw > 0) {
        GREEN_DURATION = newNs;
        RED_DURATION = newEw; // RED_DURATION acts as EW Green in this logic
        Serial.print(F(">> OK! NS_GREEN=")); Serial.print(GREEN_DURATION);
        Serial.print(F("s, EW_GREEN=")); Serial.print(RED_DURATION); Serial.println(F("s"));
      } else {
        Serial.println(F(">> Error: Invalid time values"));
      }
    }
  } else {
    Serial.println(F("? Commands: T:ns,ew | S=Status | R=Reset"));
  }
}

void printStatus() {
  const char* labels[] = {"B-N:XANH D-T:DO","B-N:VANG D-T:DO","B-N:DO D-T:XANH","B-N:DO D-T:VANG"};
  unsigned long rem = (stateDuration - (millis()-stateStartTime)) / 1000UL;
  Serial.print(F("[")); Serial.print(labels[currentState]);
  Serial.print(F("] Con lai: ")); Serial.print(rem); Serial.println(F("s"));
}