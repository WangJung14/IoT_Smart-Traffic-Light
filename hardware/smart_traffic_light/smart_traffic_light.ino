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

bool isInfiniteMode = false;
int targetState = -1; // -1 means no target, stay in infinite

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
  Serial.println(F("s | S=Status R=Reset M:0/1=Mode J:x=Jump"));
}

void loop() {
  handleSerial();
  
  // Advance state if:
  // 1. Not in infinite mode
  // OR 2. In infinite mode, but we have a target state we haven't reached yet
  // OR 3. We are currently in a Yellow state (must always transition out of yellow automatically for safety)
  
  bool shouldAdvance = (!isInfiniteMode) || 
                       (targetState != -1 && currentState != targetState) ||
                       (currentState == NS_YELLOW_EW_RED) || 
                       (currentState == NS_RED_EW_YELLOW);

  if (shouldAdvance && millis() - stateStartTime >= stateDuration) {
      nextState();
      
      // If we reached the target state, clear it so we stay here
      if (isInfiniteMode && currentState == targetState) {
          targetState = -1;
      }
  }
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
    targetState = -1;
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
  } else if (input.startsWith("F:")) {
    // Admin force state directly
    int newState = input.substring(2).toInt();
    if (newState >= 0 && newState <= 3) {
      Serial.print(F(">> ADMIN FORCE STATE: ")); Serial.println(newState);
      targetState = -1;
      enterState((TrafficState)newState);
    } else {
      Serial.println(F(">> Error: F:0-3 only"));
    }
  } else if (input.startsWith("M:")) {
    // Set Mode: M:0 (Auto), M:1 (Infinite)
    int newMode = input.substring(2).toInt();
    isInfiniteMode = (newMode == 1);
    targetState = -1; // Clear any pending transitions
    Serial.print(F(">> MODE SET TO: ")); 
    Serial.println(isInfiniteMode ? F("INFINITE") : F("AUTO"));
  } else if (input.startsWith("J:")) {
    // Jump to target GREEN state safely (through yellow).
    // Instead of waiting for current phase to expire, IMMEDIATELY
    // enter the yellow phase of the current direction so the
    // transition takes only YELLOW_DURATION (5s), not the remaining
    // green time (up to 50s).
    int t = input.substring(2).toInt();
    if (t == 0 || t == 2) {
      // Only act if we are not already in (or heading to) the target
      if (currentState != (TrafficState)t) {
        targetState = t;
        Serial.print(F("> JUMP REQUEST TO STATE: ")); Serial.println(targetState);

        // Immediately cut to the yellow of whichever direction is
        // currently green, so we only wait YELLOW_DURATION before
        // arriving at the requested state.
        if (currentState == NS_GREEN_EW_RED) {
          // NS is green -> go to NS yellow first, then EW green
          enterState(NS_YELLOW_EW_RED);
        } else if (currentState == NS_RED_EW_GREEN) {
          // EW is green -> go to EW yellow first, then NS green
          enterState(NS_RED_EW_YELLOW);
        }
        // If already in a yellow state the loop() will handle it
      } else {
        Serial.println(F("> Already at target state"));
      }
    } else {
      Serial.println(F("> Error: J:0 (NS Green) or J:2 (EW Green) only"));
    }
  } else {
    Serial.println(F("? Commands: T:ns,ew | S=Status | R=Reset | M:0/1 | F:0-3 | J:0/2"));
  }
}

void printStatus() {
  const char* labels[] = {"B-N:XANH D-T:DO","B-N:VANG D-T:DO","B-N:DO D-T:XANH","B-N:DO D-T:VANG"};
  unsigned long rem = (stateDuration - (millis()-stateStartTime)) / 1000UL;
  Serial.print(F("[")); Serial.print(labels[currentState]);
  Serial.print(F("] "));
  if (isInfiniteMode && targetState == -1 && currentState != NS_YELLOW_EW_RED && currentState != NS_RED_EW_YELLOW) {
      Serial.println(F("Con lai: INFINITE"));
  } else {
      Serial.print(F("Con lai: ")); Serial.print(rem); Serial.println(F("s"));
  }
}