#ifndef _SCPI_H_
#define _SCPI_H_

#define MAX_SCPI_CMD_LENGTH ES_MAX_LINE_LENGTH
typedef enum {Scpi_NoError = 0, Scpi_CmdHeaderError = -100, Scpi_SyntaxError = -102, Scpi_HeaderError = -110, Scpi_ExecutionError = -200, Scpi_SettingsConflict = -221, Scpi_DataOutOfRange = -222, Scpi_HardwareError = -240, Scpi_QueueOverflow = -350, Scpi_QueryError = -400} ScpiErrorCode;
//Note: adding error codes to the ScpiErrorCode enum requiers to add them to ScpiErrorCode() function as well
/* Error Codes:
0     No Error
-100  Command error - generic command parsing/execution error - when other errors do not apply 
-102  Syntax error - An unrecognized command or data type was encountered
-110  Command header error - An error was detected in the header
-200  Execution Error
-220  Parameter error - generic error - not in use
-221  Settings conflict - command could not be executed due to the current device state
-222  Data out of range
-240  Hardware error - command could not be executed because of a hardware problem in the device.

-350  Queue overflow - A specific code entered into the queue in lieu of the code that caused the erro
-400  Query error - Data in the output queue has been lost
*/
void scpiHandle(struct tcp_echoserver_struct *es);


#endif //_SCPI_H_
