################################################################################
# Automatically-generated file. Do not edit!
# Toolchain: GNU Tools for STM32 (12.3.rel1)
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
C_SRCS += \
../Core/App/Communication/Command.c \
../Core/App/Communication/IntComm.c \
../Core/App/Communication/IntUart.c \
../Core/App/Communication/IntUartDataLink.c 

OBJS += \
./Core/App/Communication/Command.o \
./Core/App/Communication/IntComm.o \
./Core/App/Communication/IntUart.o \
./Core/App/Communication/IntUartDataLink.o 

C_DEPS += \
./Core/App/Communication/Command.d \
./Core/App/Communication/IntComm.d \
./Core/App/Communication/IntUart.d \
./Core/App/Communication/IntUartDataLink.d 


# Each subdirectory must supply rules for building sources it contributes
Core/App/Communication/%.o Core/App/Communication/%.su Core/App/Communication/%.cyclo: ../Core/App/Communication/%.c Core/App/Communication/subdir.mk
	arm-none-eabi-gcc "$<" -mcpu=cortex-m3 -std=gnu11 -DUSE_HAL_DRIVER -DSTM32F103xB -c -I../Core/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc/Legacy -I../Drivers/CMSIS/Device/ST/STM32F1xx/Include -I../Drivers/CMSIS/Include -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Hardware" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/ISO_11783" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Open_SAE_J1939" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/SAE_J1939" -Os -ffunction-sections -fdata-sections -Wall -fstack-usage -fcyclomatic-complexity -MMD -MP -MF"$(@:%.o=%.d)" -MT"$@" --specs=nano.specs -mfloat-abi=soft -mthumb -o "$@"

clean: clean-Core-2f-App-2f-Communication

clean-Core-2f-App-2f-Communication:
	-$(RM) ./Core/App/Communication/Command.cyclo ./Core/App/Communication/Command.d ./Core/App/Communication/Command.o ./Core/App/Communication/Command.su ./Core/App/Communication/IntComm.cyclo ./Core/App/Communication/IntComm.d ./Core/App/Communication/IntComm.o ./Core/App/Communication/IntComm.su ./Core/App/Communication/IntUart.cyclo ./Core/App/Communication/IntUart.d ./Core/App/Communication/IntUart.o ./Core/App/Communication/IntUart.su ./Core/App/Communication/IntUartDataLink.cyclo ./Core/App/Communication/IntUartDataLink.d ./Core/App/Communication/IntUartDataLink.o ./Core/App/Communication/IntUartDataLink.su

.PHONY: clean-Core-2f-App-2f-Communication

