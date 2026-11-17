
// ==================================================
// =============== Software constants ===============
// ==================================================

#define SOFTWARE_VER_MAJOR 0;
#define SOFTWARE_VER_MINOR 1;

// --------------------------------------------------
// ==================================================


// ==================================================
// =============== Hardware constants ===============
// ==================================================

#define LED_SERIAL_IN_INDICATOR_PIN   1   // represents serial activity in - RED LED
#define LED_SERIAL_OUT_INDICATOR_PIN  2   // represents serial activity out - GREEN LED

// --------------------------------------------------
// ==================================================


// ==================================================
// ============ Communication  constants ============
// ==================================================

// Protocol constants
#define COMMS_START_BYTE_1 0xFF
#define COMMS_START_BYTE_2 0xA2
#define COMMS_STOP_BYTE 0xB3

// Operation and response codes
#define OPERATION_CALIBRATE     0xC0
#define OPERATION_SET_POS       0xC1
#define OPERATION_GOTO_POS      0xC2
#define OPERATION_SET_SERVO     0xC3
#define OPERATION_GRAB          0xC4
#define OPERATION_DROP          0xC5
#define OPERATION_LOG_POS       0xC6
#define OPERATION_REQ_VER_INFO  0xC7

#define RESPONSE_ACK            0xE0
#define RESPONSE_ERROR          0xE1  
#define RESPONSE_COMPLETE       0xE2
#define RESPONSE_STORED_POS     0xE3
#define RESPONSE_VERSION_INFO   0xE4


#define OPERATIONS_OFFSET 0xC0 // First opcode is at 0xC0 for readability and differentiation from operand / parameter values which typically do not exceed 0xB4
const uint8_t EXPECTED_OPERATIONS_PARAM_COUNT[] = {
  6,  // For Opcode 0xC0
  7,  // For Opcode 0xC1
  1,  // For Opcode 0xC2
  7,  // For Opcode 0xC3
  0,  // For Opcode 0xC4
  0,  // For Opcode 0xC5
  1,  // For Opcode 0xC6
  0,  // For Opcode 0xC7
  0,  // For Opcode 0xC8 - does not exist yet
  0,  // For Opcode 0xC9 - does not exist yet
  0,  // For Opcode 0xCA - does not exist yet
  0,  // For Opcode 0xCB - does not exist yet
  0,  // For Opcode 0xCC - does not exist yet
  0,  // For Opcode 0xCD - does not exist yet
  0,  // For Opcode 0xCE - does not exist yet
  0,  // For Opcode 0xCF - does not exist yet
  0,  // For Opcode 0xD0 - does not exist yet
  0,  // For Opcode 0xD1 - does not exist yet
  0,  // For Opcode 0xD2 - does not exist yet
  0,  // For Opcode 0xD3 - does not exist yet
  0,  // For Opcode 0xD4 - does not exist yet
  0,  // For Opcode 0xD5 - does not exist yet
  0,  // For Opcode 0xD6 - does not exist yet
  0,  // For Opcode 0xD7 - does not exist yet
  0,  // For Opcode 0xD8 - does not exist yet
  0,  // For Opcode 0xD9 - does not exist yet
  0,  // For Opcode 0xDA - does not exist yet
  0,  // For Opcode 0xDB - does not exist yet
  0,  // For Opcode 0xDC - does not exist yet
  0,  // For Opcode 0xDD - does not exist yet
  0,  // For Opcode 0xDE - does not exist yet
  0,  // For Opcode 0xDF - does not exist yet
  0,  // For Opcode 0xE0
  1,  // For Opcode 0xE1
  1,  // For Opcode 0xE2
  12, // For Opcode 0xE3
  2  // For Opcode 0xE4
};


// Error codes
enum class ErrorCode : uint8_t {
  ERROR_INVALID_PARAMETER_COUNT     = 0x00,
  ERROR_INVALID_PARAMETER_VALUE     = 0x01,
  ERROR_INVALID_OPERATION           = 0x02,
  ERROR_CONFUSING_MESSAGE           = 0x03,
  ERROR_MEMORY_LOCATION_NOT_SET     = 0x04,
  ERROR_SERVO_COMMAND_OUT_OF_RANGE  = 0x05
};

// --------------------------------------------------
// ==================================================

// Servo HS-485HB control:
// PWM - Total time 20ms
// 0.5ms = -90 degree position
// 1.5ms =   0 degree position
// 2.5ms =  90 degree position
// Formula:
// For value x, 0-180
// timeOn = 0.5 + (2 / 180)*x


void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);
  pinMode(LED_RED_PIN, OUTPUT);
  pinMode(LED_GREEN_PIN, OUTPUT);
}

void loop() {
  // put your main code here, to run repeatedly:
  digitalWrite(LED_RED_PIN, HIGH);
  digitalWrite(LED_GREEN_PIN, LOW);
  delay(100);
  digitalWrite(LED_RED_PIN, LOW);
  digitalWrite(LED_GREEN_PIN, HIGH);
  delay(100);
}

void ListenForMessages()
{

}

void ParseMessage(uint8_t opcode, uint8_t* params, uint8_t paramsCount)
{
  if (EXPECTED_OPERATIONS_PARAM_COUNT[opcode - OPERATIONS_OFFSET] != paramsCount)
  {
    RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
  }

  switch (opcode)
  {
    case OPERATION_CALIBRATE:
      // Expecting 6 parameters
      // [0] Servo offset (Base) : uint8 
      // [1] Servo offset (Shoulder) : uint8 
      // [2] Servo offset (Elbow) : uint8 
      // [3] Servo offset (Wrist) : uint8 
      // [4] Servo offset (Gripper) : uint8 
      // [5] Servo offset (Wrist rotate) : uint8 
      
      break;
    case OPERATION_SET_POS:
      // Expecting 7 parameters
      // [0] Memory index : uint8 
      // [1] Servo value (Base) : uint8 
      // [2] Servo value (Shoulder) : uint8 
      // [3] Servo value (Elbow) : uint8 
      // [4] Servo value (Wrist) : uint8 
      // [5] Servo value (Gripper) : uint8 
      // [6] Servo value (Wrist rotate) : uint8 

      break;
    case OPERATION_GOTO_POS:
      // Expecting 1 parameter
      // [0] Memory index : uint8 

      break;
    case OPERATION_SET_SERVO:
      // Expecting 7 parameters
      // [0] Servo value (Base) : uint8 
      // [1] Servo value (Shoulder) : uint8 
      // [2] Servo value (Elbow) : uint8 
      // [3] Servo value (Wrist) : uint8 
      // [4] Servo value (Gripper) : uint8 
      // [5] Servo value (Wrist rotate) : uint8 
      // [6] Apply offset : Boolean 

      break;
    case OPERATION_GRAB:
      // Expecting 0 parameters
      
      break;
    case OPERATION_DROP:
      // Expecting 0 parameters
      
      break;
    case OPERATION_LOG_POS:
      // Expecting 1 parameter
      // [0] Memory index : uint8 

      break;
    case OPERATION_REQ_VER_INFO:
      // Expecting 0 parameters
      
      break;
    default:
      RaiseError(ErrorCode::ERROR_INVALID_OPERATION);
      break;
  }
  SendMessage(.. completed message);


}



// PWM - Total time 20ms
// 0.5ms = -90 degree position
// 1.5ms =   0 degree position
// 2.5ms =  90 degree position
// Formula:
// For value x, 0-180
// timeOn = 0.5 + (2 / 180)*x
float GetTimeOnForGivenServoAngle(int x)
{
  if (x > 180)
  {
    RaiseError(ErrorCode::ERROR_SERVO_COMMAND_OUT_OF_RANGE);
    return 2.5f; // maximum value
  }
  return 0.5f + (2.0f*x/180);
}

void RaiseError(ErrorCode error)
{
  // note error and raise via communications
  SendMessage(...);
}

void SendMessage(uint8_t* message, uint8_t length)
{
  digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, HIGH);

  Serial.print(COMMS_START_BYTE_1);
  Serial.print(COMMS_START_BYTE_2);
  for (int i = 0; i < length; i++)
  {
    Serial.print(message[i]);
  }
  Serial.print(COMMS_STOP_BYTE);

  digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, LOW);
}

bool EvaluateBooleanUint(uint8_t value)
{
  if (value == 1) return true;
  else if (value == 0) return false;
  RaiseError(ErrorCode::ERROR_INVALID_OPERATION);
  return false;
}

