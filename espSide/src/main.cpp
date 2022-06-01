#include <Arduino.h>
#include <WiFi.h>

#define moisture_sens 34

const int airValue = 3560;
const int waterValue = 1650;
const int sensorReadInterval = 20000;
const int unReasonableValue = 10;
const int roomID = 1;

const char* ssid = "ACAGuest";
const char* password = "FramtidNu";

int sensorValue = 0;
int tempValue = 0;
int intervalCounter = 0;
int securityCounter = 0;
bool waterLeak = false;

int sensorRead();

void initWiFi();

void setup() {
  Serial.begin(9600);
  initWiFi();
  Serial.println();
}

void loop() {
  tempValue = sensorRead();
  if(tempValue > unReasonableValue)
  {
    Serial.print("SecCounter: ");
    Serial.println(securityCounter);
    Serial.print("intervalCounter: ");
    Serial.println(intervalCounter);
    securityCounter += 1;
    if(securityCounter == 1){
      intervalCounter = 0;
    }
    if(securityCounter >= 3)
    {
      Serial.print("percentage: ");
      Serial.print(tempValue);
      Serial.println(" % WARNING WATER LEAK");
      waterLeak = true;
    }
  }
  else{
    Serial.print("percentage: ");
    Serial.print(tempValue);
    Serial.println(" %");
  }
  if(intervalCounter == 3 && waterLeak == false)
  {
    intervalCounter = 0;
    securityCounter = 0;
  }
  intervalCounter += 1;
  delay(sensorReadInterval);
}
    
int sensorRead()
{
  sensorValue = analogRead(moisture_sens);
  int percentValue = map(sensorValue,airValue, waterValue, 0, 100);

  Serial.print("Sensor = ");
  Serial.print(sensorValue);
  Serial.print(" - ");
  return(percentValue);
}

void initWiFi() {
  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);
  Serial.print("Connecting to WiFi ..");
  while (WiFi.status() != WL_CONNECTED) {
    Serial.print('.');
    delay(1000);
  }
  Serial.println(WiFi.localIP());
}