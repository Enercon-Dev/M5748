################################################################################
# Automatically-generated file. Do not edit!
# Toolchain: GNU Tools for STM32 (12.3.rel1)
################################################################################

# Add inputs and outputs from these tool invocations to the build variables 
C_SRCS += \
../Core/App/DBG.c \
../Core/App/DataBuffer.c \
../Core/App/IO.c \
../Core/App/Mgmt.c \
../Core/App/RowIO.c \
../Core/App/Timing.c \
../Core/App/flash_if.c \
../Core/App/general.c 

OBJS += \
./Core/App/DBG.o \
./Core/App/DataBuffer.o \
./Core/App/IO.o \
./Core/App/Mgmt.o \
./Core/App/RowIO.o \
./Core/App/Timing.o \
./Core/App/flash_if.o \
./Core/App/general.o 

C_DEPS += \
./Core/App/DBG.d \
./Core/App/DataBuffer.d \
./Core/App/IO.d \
./Core/App/Mgmt.d \
./Core/App/RowIO.d \
./Core/App/Timing.d \
./Core/App/flash_if.d \
./Core/App/general.d 


# Each subdirectory must supply rules for building sources it contributes
Core/App/%.o Core/App/%.su Core/App/%.cyclo: ../Core/App/%.c Core/App/subdir.mk
	arm-none-eabi-gcc "$<" -mcpu=cortex-m3 -std=gnu11 -DUSE_HAL_DRIVER -DSTM32F103xB -c -I../Core/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc -I../Drivers/STM32F1xx_HAL_Driver/Inc/Legacy -I../Drivers/CMSIS/Device/ST/STM32F1xx/Include -I../Drivers/CMSIS/Include -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Hardware" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/ISO_11783" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/Open_SAE_J1939" -I"C:/Projects/M5748/M5748_COM/CUBE IDE/Core/App/Communication/CAN/SAE_J1939" -Os -ffunction-sections -fdata-sections -Wall -fstack-usage -fcyclomatic-complexity -MMD -MP -MF"$(@:%.o=%.d)" -MT"$@" --specs=nano.specs -mfloat-abi=soft -mthumb -o "$@"

clean: clean-Core-2f-App

clean-Core-2f-App:
	-$(RM) ./Core/App/DBG.cyclo ./Core/App/DBG.d ./Core/App/DBG.o ./Core/App/DBG.su ./Core/App/DataBuffer.cyclo ./Core/App/DataBuffer.d ./Core/App/DataBuffer.o ./Core/App/DataBuffer.su ./Core/App/IO.cyclo ./Core/App/IO.d ./Core/App/IO.o ./Core/App/IO.su ./Core/App/Mgmt.cyclo ./Core/App/Mgmt.d ./Core/App/Mgmt.o ./Core/App/Mgmt.su ./Core/App/RowIO.cyclo ./Core/App/RowIO.d ./Core/App/RowIO.o ./Core/App/RowIO.su ./Core/App/Timing.cyclo ./Core/App/Timing.d ./Core/App/Timing.o ./Core/App/Timing.su ./Core/App/flash_if.cyclo ./Core/App/flash_if.d ./Core/App/flash_if.o ./Core/App/flash_if.su ./Core/App/general.cyclo ./Core/App/general.d ./Core/App/general.o ./Core/App/general.su

.PHONY: clean-Core-2f-App

