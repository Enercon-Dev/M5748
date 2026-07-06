################################################################################
# Automatically-generated file. Do not edit!
# Toolchain: GNU Tools for STM32 (12.3.rel1)
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
C_SRCS += \
../Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.c \
../Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.c \
../Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.c 

OBJS += \
./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.o \
./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.o \
./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.o 

C_DEPS += \
./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.d \
./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.d \
./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.d 


# Each subdirectory must supply rules for building sources it contributes
Core/App/Communication/CAN/Open_SAE_J1939/%.o Core/App/Communication/CAN/Open_SAE_J1939/%.su Core/App/Communication/CAN/Open_SAE_J1939/%.cyclo: ../Core/App/Communication/CAN/Open_SAE_J1939/%.c Core/App/Communication/CAN/Open_SAE_J1939/subdir.mk
	arm-none-eabi-gcc "$<" -mcpu=cortex-m3 -std=gnu11 -DUSE_HAL_DRIVER -DSTM32F103xB -c -I../Core/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc/Legacy -I../Drivers/CMSIS/Device/ST/STM32F1xx/Include -I../Drivers/CMSIS/Include -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Hardware" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/ISO_11783" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Open_SAE_J1939" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/SAE_J1939" -Os -ffunction-sections -fdata-sections -Wall -fstack-usage -fcyclomatic-complexity -MMD -MP -MF"$(@:%.o=%.d)" -MT"$@" --specs=nano.specs -mfloat-abi=soft -mthumb -o "$@"

clean: clean-Core-2f-App-2f-Communication-2f-CAN-2f-Open_SAE_J1939

clean-Core-2f-App-2f-Communication-2f-CAN-2f-Open_SAE_J1939:
	-$(RM) ./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.cyclo ./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.d ./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.o ./Core/App/Communication/CAN/Open_SAE_J1939/Closedown_ECU.su ./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.cyclo ./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.d ./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.o ./Core/App/Communication/CAN/Open_SAE_J1939/Listen_For_Messages.su ./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.cyclo ./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.d ./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.o ./Core/App/Communication/CAN/Open_SAE_J1939/Startup_ECU.su

.PHONY: clean-Core-2f-App-2f-Communication-2f-CAN-2f-Open_SAE_J1939

