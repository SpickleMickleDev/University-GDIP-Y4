#include <Preferences.h> // Non-volatile storage

// Optional compilation symbols
#define DEBUG


// ==================================================
// =============== Software constants ===============
// ==================================================

#define SOFTWARE_VER_MAJOR 1;
#define SOFTWARE_VER_MINOR 1;

// --------------------------------------------------
// ==================================================


// ==================================================
// =============== Hardware constants ===============
// ==================================================

const uint8_t LED_SERIAL_IN_INDICATOR_PIN = 1;   // represents serial activity in - RED LED
const uint8_t LED_SERIAL_OUT_INDICATOR_PIN = 2;   // represents serial activity out - GREEN LED

const uint8_t SERVO_BASE_PWM_PIN         = 39;
const uint8_t SERVO_SHOULDER_PWM_PIN     = 37;
const uint8_t SERVO_ELBOW_PWM_PIN        = 35;
const uint8_t SERVO_WRIST_PWM_PIN        = 33;
const uint8_t SERVO_WRIST_ROTATE_PWM_PIN = 18;
const uint8_t SERVO_GRIPPER_PWM_PIN      = 16;
enum class ServoIndices : uint8_t {
  BASE,
  SHOULDER,
  ELBOW,
  WRIST,
  WRIST_ROTATE,
  GRIPPER,
  COUNT
};
constexpr uint8_t SERVO_COUNT = static_cast<uint8_t>(ServoIndices::COUNT);

constexpr uint8_t SERVO_PINS[SERVO_COUNT] = {
  SERVO_BASE_PWM_PIN,
  SERVO_SHOULDER_PWM_PIN,
  SERVO_ELBOW_PWM_PIN,
  SERVO_WRIST_PWM_PIN,
  SERVO_WRIST_ROTATE_PWM_PIN,
  SERVO_GRIPPER_PWM_PIN
};

const uint8_t SERVO_PWM_FREQUENCY = 50;
const uint8_t SERVO_PWM_RESOLUTION = 12;

// Values set for maximum safe range of motion
const uint8_t SERVO_POSITION_LIMITS[][2] = 
{
  {0x00, 0xCD}, // Servo 1 min, max
  {0x00, 0xCD}, // Servo 2 min, max
  {0x00, 0xCD}, // Servo 3 min, max
  {0x00, 0xCD}, // Servo 4 min, max
  {0x00, 0xCD}, // Servo 5 min, max
  {0x00, 0xCD}  // Servo 6 min, max
};

// --------------------------------------------------
// ==================================================


// ==================================================
// =============== Positional Storage ===============
// ==================================================

const uint8_t MAX_POSITION_COUNT = 256;
#define FILE_NAME_POSITIONS "/positions.txt"
#define FILE_NAME_OFFSETS "/offsets.txt"

Preferences NVMStorage;
uint8_t positionData[MAX_POSITION_COUNT][SERVO_COUNT];
// We want this data to persist. Use LittleFS
// Will look like:
// {
//    { <servo 1 value>, <servo 2 value>, <servo 3 value>, <servo 4 value>, <servo 5 value>, <servo 6 value> },
//    { <servo 1 value>, <servo 2 value>, <servo 3 value>, <servo 4 value>, <servo 5 value>, <servo 6 value> },
//    { <servo 1 value>, <servo 2 value>, <servo 3 value>, <servo 4 value>, <servo 5 value>, <servo 6 value> },
//     ...
//    { <servo 1 value>, <servo 2 value>, <servo 3 value>, <servo 4 value>, <servo 5 value>, <servo 6 value> }
// }

// Stores the offsets for each servo to account for any bias at the software level
int8_t offsets[SERVO_COUNT];

// Stores the current position data for reference
uint8_t currentPosition[SERVO_COUNT];

// --------------------------------------------------
// ==================================================


// ==================================================
// ============ Communication  constants ============
// ==================================================

// Protocol constants
const uint8_t COMMS_START_BYTE_1 =        0xFF;
const uint8_t COMMS_START_BYTE_2 =        0xF6;
const uint8_t COMMS_STOP_BYTE    =        0x5F;

// Operation and response codes
const uint8_t OPERATION_CALIBRATE =       0xD0;
const uint8_t OPERATION_SET_POS =         0xD1;
const uint8_t OPERATION_GOTO_POS =        0xD2;
const uint8_t OPERATION_SET_SERVO =       0xD3;
const uint8_t OPERATION_GRAB =            0xD4;
const uint8_t OPERATION_DROP =            0xD5;
const uint8_t OPERATION_LOG_POS =         0xD6;
const uint8_t OPERATION_REQ_VER_INFO =    0xD7;

const uint8_t RESPONSE_ACK =              0xE0;
const uint8_t RESPONSE_ERROR =            0xE1;  
const uint8_t RESPONSE_COMPLETE =         0xE2;
const uint8_t RESPONSE_STORED_POS =       0xE3;
const uint8_t RESPONSE_VERSION_INFO =     0xE4;
const uint8_t RESPONSE_GENERAL_MESSAGE =  0xE5;


const uint8_t OPERATIONS_OFFSET = 0xD0; // First opcode is at 0xD0 for readability and differentiation from operand / parameter values which typically do not exceed 0xCD
const uint8_t EXPECTED_OPERATIONS_PARAM_COUNT[] = {
  6,  // For Opcode 0xD0
  7,  // For Opcode 0xD1
  1,  // For Opcode 0xD2
  7,  // For Opcode 0xD3
  0,  // For Opcode 0xD4
  0,  // For Opcode 0xD5
  1,  // For Opcode 0xD6
  0,  // For Opcode 0xD7
  0,  // For Opcode 0xD8 - does not exist yet
  0,  // For Opcode 0xD9 - does not exist yet
  0,  // For Opcode 0xDA - does not exist yet
  0,  // For Opcode 0xDB - does not exist yet
  0,  // For Opcode 0xDC - does not exist yet
  0,  // For Opcode 0xDD - does not exist yet
  0,  // For Opcode 0xDE - does not exist yet
  0,  // For Opcode 0xDF - does not exist yet
};


// Error codes
enum class ErrorCode : uint8_t {
  ERROR_INVALID_PARAMETER_COUNT     = 0x00,
  ERROR_INVALID_PARAMETER_VALUE     = 0x01,
  ERROR_INVALID_OPERATION           = 0x02,
  ERROR_CONFUSING_MESSAGE           = 0x03,
  ERROR_MEMORY_LOCATION_NOT_SET     = 0x04,
  ERROR_SERVO_COMMAND_OUT_OF_RANGE  = 0x05,
  ERROR_SERVO_W_OFFSET_OUT_OF_RANGE = 0x06
};

#define MAX_MESSAGE_LENGTH 255

#ifdef DEBUG
#define DEBUG_MESSAGE(format, ...) \
    do { \
        char debugBuffer[MAX_MESSAGE_LENGTH + 1]; \
        int debugLength = snprintf(debugBuffer, sizeof(debugBuffer), format, ##__VA_ARGS__); \
        if (debugLength < 0) { \
            break; \
        } \
        if (debugLength > MAX_MESSAGE_LENGTH) debugLength = MAX_MESSAGE_LENGTH; \
        SendGeneralMessage(debugBuffer, (uint8_t)debugLength); \
    } while (0) // backslashes allow macro across multiple lines and do while(0) prevents unexpected behaviour with if else branches
#else
// If DEBUG is not defined then do nothing
#define DEBUG_MESSAGE(...) ((void)0);
#endif

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

// LEDc API can facilitate hardware PWM control of multiple pins


void setup() {
  // put your setup code here, to run once:
  Serial.begin(115200);
  pinMode(LED_SERIAL_IN_INDICATOR_PIN, OUTPUT);
  pinMode(LED_SERIAL_OUT_INDICATOR_PIN, OUTPUT);

  for (int i = 0; i < SERVO_COUNT; i++)
  {
    ledcAttach(SERVO_PINS[i],          SERVO_PWM_FREQUENCY,  SERVO_PWM_RESOLUTION);
  }
  // T = 20ms   f = 1/T = 1/0.02 = 50Hz
  // 0.5ms-2.5ms / 20ms
  // Duty cycle 2.5%-12.5%
  // Therefore servo resolution = 10% of configured PWM resolution
  // 9 bits = 20%, 10 bits = 40%, 11 bits = 80%, 12 bits = 160% (We need 12 bits)
  // 2^12 = 4096
  // Between 2.5%-12.5% that is 409(.6) bits of selection. Need to scale this. Allow 0x00-0xBF inclusive, [0x00,0xC0)
  // Allow 0x00-0xCD inclusive

  // Load positions from non volatile memory
  if (!LoadPositionsMatrices())
  {
    DEBUG_MESSAGE("Position matrix failed to load. Building a new one.");
   
    memset(positionData, 0, sizeof(positionData));
    SavePositionsMatrices();
  }
  
  if (!LoadOffsets())
  {
    DEBUG_MESSAGE("Offsets failed to load. Building new values.");

    memset(offsets, 0, sizeof(offsets));
    SaveOffsets();
  }
}

void loop() 
{
#ifdef DEBUG
unsigned long before = 0;
unsigned long after = 0;
#endif

  if (Serial.available() > 0)
  {
    digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, HIGH);

    #ifdef DEBUG
    before = micros();
    #endif
    
    DEBUG_MESSAGE("Starting process messages");
    ProcessMessages();

    #ifdef DEBUG
    after = micros();
    DEBUG_MESSAGE("Processing message took %dus.", after - before); // Print how many microseconds the ProcessMessages() function took
    #endif
    digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, LOW);
  }

  //delay(100);
  //DEBUG_MESSAGE("Is anyone there?");
}

void debugLoop()
{
  digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, HIGH);
  digitalWrite(LED_SERIAL_OUT_INDICATOR_PIN, LOW);
  delay(100);
  digitalWrite(LED_SERIAL_IN_INDICATOR_PIN, LOW);
  digitalWrite(LED_SERIAL_OUT_INDICATOR_PIN, HIGH);
  delay(100);
}

bool SavePositionsMatrices()
{
  if (!NVMStorage.begin("Storage", false)) return false;

  size_t writtenPosData = NVMStorage.putBytes(FILE_NAME_POSITIONS, positionData, sizeof(positionData));
  NVMStorage.end();

  return (writtenPosData == sizeof(positionData));
 
  // Simple as that.
}

bool LoadPositionsMatrices()
{
  if (!NVMStorage.begin("Storage", true)) return false;

  size_t sizePosData = NVMStorage.getBytesLength(FILE_NAME_POSITIONS);

  if (sizePosData != sizeof(positionData))
  {
    NVMStorage.end();
    return false;
  }

  size_t readPosData = NVMStorage.getBytes(FILE_NAME_POSITIONS, positionData, sizeof(positionData));
  NVMStorage.end();

  return (readPosData == sizeof(positionData));
}

bool SaveOffsets()
{
  if (!NVMStorage.begin("Storage", false)) return false;

  size_t writtenOffsets = NVMStorage.putBytes(FILE_NAME_OFFSETS, offsets, sizeof(offsets));
  NVMStorage.end();

  return (writtenOffsets == sizeof(offsets));
}

bool LoadOffsets()
{
  if (!NVMStorage.begin("Storage", true)) return false;

  size_t sizeOffsets = NVMStorage.getBytesLength(FILE_NAME_OFFSETS);

  if (sizeOffsets != sizeof(offsets))
  {
    NVMStorage.end();
    return false;
  }

  size_t readOffsets = NVMStorage.getBytes(FILE_NAME_OFFSETS, offsets, sizeof(offsets));
  NVMStorage.end();

  return (readOffsets == sizeof(offsets));
}

void ProcessMessages()
{
  // by now we already know the serial buffer is populated somewhat 
  unsigned long timeoutMS = 1000; // 1 second timeout to process the message 
  uint8_t currentByte; // Message should be read byte by byte
  int state = 0;
  int parameterCount = 0;
  int parametersAdded = 0;
  bool messageFinished = false;
  uint8_t* message = nullptr; // initialise pointer - allocate memory later when we know the amount

  unsigned long operationStart = millis();
  while (millis() - operationStart < timeoutMS && !messageFinished)
  {
    Serial.read(&currentByte, sizeof(uint8_t)); // Read just one byte
    DEBUG_MESSAGE("Start of while loop. Byte is (0x%02x). Elapsed time is %ul ms", currentByte, millis() - operationStart);

    switch (state)
    {
      case 0: // Looking for start byte 1
        if (currentByte == COMMS_START_BYTE_1)
        {
          state = 1;
          DEBUG_MESSAGE("Moving to state 1");
        }
        break;

      case 1: // Looking for start byte 2
        if (currentByte == COMMS_START_BYTE_2)
        {
          state = 2;
          DEBUG_MESSAGE("Moving to state 2");
        }
        else if (currentByte != COMMS_START_BYTE_1)
        {
          state = 0;
          DEBUG_MESSAGE("Moving back to state 0 from 1");
        }
        // If we find start byte 2 here (as expected), move onto parsing the rest of the message
        // If we find start byte 1 here, indicates the start of a new message (Weird but at least prepare for it)
        // If we find some other random byte here, scrap it all and go back to looking for the first start byte
        break;

      case 2: // Reading operation code - once we know this, we know the expected number of parameters
        parameterCount = EXPECTED_OPERATIONS_PARAM_COUNT[currentByte - OPERATIONS_OFFSET];
        message = (uint8_t*)malloc((parameterCount + 1) * sizeof(uint8_t)); // unfinished line. Also yet to do case 3. Do i malloc here?
        message[0] = currentByte;
        state = 3;
        
        if (message == nullptr) 
        {
          DEBUG_MESSAGE("Attempting to raise confusing message error");
          RaiseError(ErrorCode::ERROR_CONFUSING_MESSAGE);
        }
        break;

      case 3: // Read parameters
        if (currentByte != COMMS_STOP_BYTE)
        {
          parametersAdded++;
          if (parametersAdded > parameterCount) // We have more parameters than expected
          {
            DEBUG_MESSAGE("Attempting to raise invalid param count error");
            RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
            messageFinished = true;
          }
          else
          {
            message[parametersAdded] = currentByte;
          }
        }
        else
        {
          if (parametersAdded < parameterCount) // We have fewer parameters than expected
          {
            DEBUG_MESSAGE("Attempting to raise invalid param count error");
            RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
          }
          else
          {
            // Now do the respective operation
            DEBUG_MESSAGE("About to send ACK");
            SendMessageAck();
            DEBUG_MESSAGE("Just sent ACK");

            switch(message[0]) // operation code
            {
              case OPERATION_CALIBRATE:
                DEBUG_MESSAGE("Calibrate");
                OperationCall_Calibrate(&message[1], parametersAdded);
                break;
              case OPERATION_SET_POS:
                DEBUG_MESSAGE("Set pos");
                OperationCall_SetPosition(&message[1], parametersAdded);
                break;
              case OPERATION_GOTO_POS:
                DEBUG_MESSAGE("Goto pos");
                OperationCall_GoToPosition(message[1]);
                break;
              case OPERATION_SET_SERVO:
                DEBUG_MESSAGE("Set servo");
                OperationCall_SetServos(&message[1], parametersAdded);
                break;
              case OPERATION_GRAB:
                DEBUG_MESSAGE("Grab");
                OperationCall_Grab();
                break;
              case OPERATION_DROP:
                DEBUG_MESSAGE("Drop");
                OperationCall_Drop();
                break;
              case OPERATION_LOG_POS:
                DEBUG_MESSAGE("Log pos");
                OperationCall_LogPosition(message[1]);
                break;
              case OPERATION_REQ_VER_INFO:
                DEBUG_MESSAGE("Calibrate");
                OperationCall_VersionInfoRequested();
                break;
              default:
                DEBUG_MESSAGE("Invalid operation error");
                RaiseError(ErrorCode::ERROR_INVALID_OPERATION);
                free(message); // Because we are using return to leave early, but still need to de-allocate the memory.
                return;
            }
            DEBUG_MESSAGE("Sending operation completed message");
            SendOperationCompletedMessage(message[0]);
          }
          messageFinished = true;
        }
        break;
    }
  }
  DEBUG_MESSAGE("Out of while loop now");
  if (message != nullptr) free(message);
}

// Expecting 6 parameters
// [0] Servo offset (Base) : int8 
// [1] Servo offset (Shoulder) : int8 
// [2] Servo offset (Elbow) : int8 
// [3] Servo offset (Wrist) : int8 
// [4] Servo offset (Wrist rotate) : int8 
// [5] Servo offset (Gripper) : int8 
// Instructs the robot arm circuit to use the following offsets for the servo values. 
void OperationCall_Calibrate(uint8_t* parameters, uint8_t parameterCount)
{
  DEBUG_MESSAGE("Calibrate message received with %d parameters: %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x)", parameterCount, parameters[0], parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5]);
  if (parameterCount != 6)
  {
    RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
    return;
  }

  int8_t signedOffsets[SERVO_COUNT];

  for (size_t i = 0; i < SERVO_COUNT; i++) {
    signedOffsets[i] = static_cast<int8_t>(parameters[i]);
  }
  // @todo: Yet to test if signed offsets and such actually work
  memcpy(offsets, signedOffsets, 6);
  SaveOffsets();

  DEBUG_MESSAGE("Offsets now set to %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x).", parameters[0], parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5]);
}

// Expecting 7 parameters
// [0] Memory index : uint8 
// [1] Servo value (Base) : uint8 
// [2] Servo value (Shoulder) : uint8 
// [3] Servo value (Elbow) : uint8 
// [4] Servo value (Wrist) : uint8 
// [5] Servo value (Wrist rotate) : uint8 
// [6] Servo value (Gripper) : uint8 
// Saves a set position in memory that can be referenced later. 
void OperationCall_SetPosition(uint8_t* parameters, uint8_t parameterCount)
{
  DEBUG_MESSAGE("Set position message received with %d parameters:  %d(0x%02x), %d(0x%02x), %d(0x%02x), %d(0x%02x), %d(0x%02x), %d(0x%02x), %d(0x%02x), %d(0x%02x)", parameterCount, parameters[0], parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5], parameters[6], parameters[6]);
  if (parameterCount != 7)
  {
    RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
    return;
  }

  memcpy(positionData[parameters[0]], &parameters[1], 6 * sizeof(uint8_t));

  DEBUG_MESSAGE("Position data at address %d now set to %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x).", parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5], parameters[6], parameters[6]);
}

// Expecting 1 parameter
// [0] Memory index : uint8 
// Loads a set position from memory that was previously set. 
// Will raise an error if that memory location has not yet been set. 
void OperationCall_GoToPosition(uint8_t index)
{
  DEBUG_MESSAGE("Goto position message received with parameter:  %d(%02x)", index);
  // @todo Implement moving the servos. Need to add bound checks of course, also factoring in offsets. Possible values [0x00,0xCD]
  for (int i = 0; i < SERVO_COUNT; i++)
  {
    SetServoPosition(i, positionData[index][i], true);
  }

  DEBUG_MESSAGE("Servos moving to %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x).", positionData[index][0], positionData[index][0], positionData[index][1], positionData[index][1], positionData[index][2], positionData[index][2], positionData[index][3], positionData[index][3], positionData[index][4], positionData[index][4], positionData[index][5], positionData[index][5]);
}

// Expecting 7 parameters
// [0] Servo value (Base) : uint8 
// [1] Servo value (Shoulder) : uint8 
// [2] Servo value (Elbow) : uint8 
// [3] Servo value (Wrist) : uint8 
// [4] Servo value (Wrist rotate) : uint8 
// [5] Servo value (Gripper) : uint8 
// [6] Apply offset : Boolean
// Instructs the robot arm circuit to move to a desired position, dictated by the following servo values. 
// The Apply offset parameter determines if the calibrated offset is to be applied. 
// This is since if one is looking to determine the correct calibration offsets, it would be ideal not to add the existing offsets. 
void OperationCall_SetServos(uint8_t* parameters, uint8_t parameterCount)
{
  DEBUG_MESSAGE("Set servos message received with %d parameters:  %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x)", parameterCount, parameters[0], parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5], parameters[6], parameters[6]);
  if (parameterCount != 7)
  {
    RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_COUNT);
    return;
  }

  bool useOffset = (parameters[6] == 0x01);

  for (int i = 0; i < SERVO_COUNT; i++)
  {
    SetServoPosition(i, parameters[i], useOffset);
  }

  DEBUG_MESSAGE("Servos manually moved to %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x), %d(%02x). Offset is%s applied.", parameters[0], parameters[0], parameters[1], parameters[1], parameters[2], parameters[2], parameters[3], parameters[3], parameters[4], parameters[4], parameters[5], parameters[5], useOffset ? "" : " not");
}

// 0 Parameters expected.
// Instructs the robot arm to drop its current item (i.e. Set the Gripper servo value to open) 
void OperationCall_Grab()
{
  DEBUG_MESSAGE("Grab message received");

  SetServoPosition(static_cast<uint8_t>(ServoIndices::GRIPPER), SERVO_POSITION_LIMITS[static_cast<uint8_t>(ServoIndices::GRIPPER)][0], true);
  // Assuming gripped servo opens when set to min value. Needs experimenting

  DEBUG_MESSAGE("Item has been grabbed.");
}

// 0 Parameters expected.
// Instructs the robot arm to drop its current item (i.e. Set the Gripper servo value to open) 
void OperationCall_Drop()
{
  DEBUG_MESSAGE("Drop message received");
  SetServoPosition(static_cast<uint8_t>(ServoIndices::GRIPPER), SERVO_POSITION_LIMITS[static_cast<uint8_t>(ServoIndices::GRIPPER)][1], true);
  // Assuming gripped servo opens when set to max value. Needs experimenting

  DEBUG_MESSAGE("Item has been dropped.");
}

// Expecting 1 parameter
// [0] Memory index : uint8 
// Returns:
// [0] Servo value (Base) : uint8 
// [1] Servo value (Shoulder) : uint8 
// [2] Servo value (Elbow) : uint8 
// [3] Servo value (Wrist) : uint8 
// [4] Servo value (Wrist rotate) : uint8 
// [5] Servo value (Gripper) : uint8 
// [6] Servo offset (Base) : uint8 
// [7] Servo offset (Shoulder) : uint8 
// [8] Servo offset (Elbow) : uint8 
// [9] Servo offset (Wrist) : uint8 
// [10] Servo offset (Wrist rotate) : uint8 
// [11] Servo offset (Gripper) : uint8 
// Instructs the embedded software to log (respond with) the stored position values at a given stored location, for verification purposes. 
void OperationCall_LogPosition(uint8_t index)
{
  DEBUG_MESSAGE("Log position message received with parameter:  %d(%02x)", index);

  uint8_t message[13] = {RESPONSE_STORED_POS, currentPosition[0], currentPosition[1], currentPosition[2], currentPosition[3], currentPosition[4], currentPosition[5], offsets[0], offsets[1], offsets[2], offsets[3], offsets[4], offsets[5]};

  SendMessage(message, 13);
}

// 0 Parameters expected.
// Returns:
// [0] Major version number : uint8 
// [1] Minor version num ber : uint8 
// Requests version info from the embedded software 
void OperationCall_VersionInfoRequested()
{
  DEBUG_MESSAGE("Version info request message received");

  uint8_t* message = (uint8_t*)malloc(3 * sizeof(uint8_t));
  message[0] = RESPONSE_VERSION_INFO;
  message[1] = SOFTWARE_VER_MAJOR;
  message[2] = SOFTWARE_VER_MINOR;

  SendMessage(message, 3);
  free(message);
}

void SendMessageAck()
{
  uint8_t* message = (uint8_t*)malloc(sizeof(uint8_t));
  message[0] = RESPONSE_ACK;

  SendMessage(message, 1);
  free(message);
}

void SendOperationCompletedMessage(uint8_t operationCode)
{
  uint8_t* message = (uint8_t*)malloc(2 * sizeof(uint8_t));
  message[0] = RESPONSE_COMPLETE;
  message[1] = operationCode;

  SendMessage(message, 2);
  free(message);
}

bool SendGeneralMessage(char* messageToSend, uint8_t messageLength)
{
  if (messageLength > 0xFF) return false;
  if (messageLength == 0) return true;

  
  uint8_t* message = (uint8_t*)malloc(sizeof(uint8_t) + messageLength * sizeof(char)); // Same size but just for consistency really
  message[0] = RESPONSE_GENERAL_MESSAGE;
  memcpy(&message[1], messageToSend, messageLength);

  SendMessage(message, messageLength + 1);
  free(message);
}

bool SetServoPosition(uint8_t servoIndex, uint8_t value, bool useOffset) // value from 0-205 (0x00-0xCD)
// Returns if the operation was valid or not
{
  int16_t correctedValue = value;
  if (useOffset) correctedValue += offsets[servoIndex]; // faster when compiled than summing with a shorthand if statement

  if (correctedValue > SERVO_POSITION_LIMITS[servoIndex][1]) // If greater than this servo's set maximum value
  {
    DEBUG_MESSAGE("Servo request too high. Value: %d, Corrected value: %d, Limit: %d. index is %d, useOffset is %d", value, correctedValue, servoIndex, SERVO_POSITION_LIMITS[servoIndex][1], useOffset);
    RaiseError(ErrorCode::ERROR_SERVO_W_OFFSET_OUT_OF_RANGE);
  }
  else if (correctedValue < SERVO_POSITION_LIMITS[servoIndex][0]) // If lower than this servo's set minimum value
  {
    DEBUG_MESSAGE("Servo request too low. Value: %d, Corrected value: %d, Limit: %d, index is %d, useOffset is %d", value, correctedValue, servoIndex, SERVO_POSITION_LIMITS[servoIndex][0], useOffset);
    RaiseError(ErrorCode::ERROR_SERVO_W_OFFSET_OUT_OF_RANGE);
  }
  else if (servoIndex >= SERVO_COUNT)
  {
    DEBUG_MESSAGE("Servo request has invalid servo index");
    RaiseError(ErrorCode::ERROR_INVALID_PARAMETER_VALUE);
  }
  else
  {
    // Total resolution = 12 bits = 4096 total
    // Duty cycle varies from 2.49%(102) to 12.5%(512). 
    // The servo therefore has a range of 410 possible values. 'uint8_t value' varies from 0 - 205, making the lowest value step 0.0488%
    ledcWrite(SERVO_PINS[servoIndex], 102 + correctedValue);

    // Update current position values
    currentPosition[servoIndex] = value;
    return true;
  }
  return false;
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
  uint8_t* message = (uint8_t*)malloc(2 * sizeof(uint8_t));
  message[0] = RESPONSE_ERROR;
  message[1] = (uint8_t)error;

  SendMessage(message, 2);
  free(message);
}

void SendMessage(uint8_t* message, uint8_t length)
{
  digitalWrite(LED_SERIAL_OUT_INDICATOR_PIN, HIGH);

  Serial.write(COMMS_START_BYTE_1);
  Serial.write(COMMS_START_BYTE_2);
  for (int i = 0; i < length; i++)
  {
    Serial.write(message[i]);
  }
  Serial.write(COMMS_STOP_BYTE);

  digitalWrite(LED_SERIAL_OUT_INDICATOR_PIN, LOW);
}

bool EvaluateBooleanUint(uint8_t value)
{
  if (value == 1) return true;
  else if (value == 0) return false;
  RaiseError(ErrorCode::ERROR_INVALID_OPERATION);
  return false;
}

