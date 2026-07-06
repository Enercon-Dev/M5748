################################################################################
# Automatically-generated file. Do not edit!
# Toolchain: GNU Tools for STM32 (12.3.rel1)
################################################################################

-include ../makefile.init

RM := rm -rf

# All of the sources participating in the build are defined here
-include sources.mk
-include Drivers/STM32F1xx_HAL_Driver/Src/subdir.mk
-include Core/Src/subdir.mk
-include Core/App/Communication/CAN/SAE_J1939/SAE_J1939-81_Network_Management_Layer/subdir.mk
-include Core/App/Communication/CAN/SAE_J1939/SAE_J1939-73_Diagnostics_Layer/subdir.mk
-include Core/App/Communication/CAN/SAE_J1939/SAE_J1939-71_Application_Layer/subdir.mk
-include Core/App/Communication/CAN/SAE_J1939/SAE_J1939-21_Transport_Layer/subdir.mk
-include Core/App/Communication/CAN/Open_SAE_J1939/subdir.mk
-include Core/App/Communication/CAN/ISO_11783/ISO_11783-7_Application_Layer/subdir.mk
-include Core/App/Communication/CAN/Hardware/subdir.mk
-include Core/App/Communication/CAN/subdir.mk
-include Core/App/Communication/subdir.mk
-include Core/App/subdir.mk
-include objects.mk

ifneq ($(MAKECMDGOALS),clean)
ifneq ($(strip $(S_DEPS)),)
-include $(S_DEPS)
endif
ifneq ($(strip $(S_UPPER_DEPS)),)
-include $(S_UPPER_DEPS)
endif
ifneq ($(strip $(C_DEPS)),)
-include $(C_DEPS)
endif
endif

-include ../makefile.defs

OPTIONAL_TOOL_DEPS := \
$(wildcard ../makefile.defs) \
$(wildcard ../makefile.init) \
$(wildcard ../makefile.targets) \


BUILD_ARTIFACT_NAME := M5748_COM
BUILD_ARTIFACT_EXTENSION := elf
BUILD_ARTIFACT_PREFIX :=
BUILD_ARTIFACT := $(BUILD_ARTIFACT_PREFIX)$(BUILD_ARTIFACT_NAME)$(if $(BUILD_ARTIFACT_EXTENSION),.$(BUILD_ARTIFACT_EXTENSION),)

# Add inputs and outputs from these tool invocations to the build variables 
EXECUTABLES += \
M5748_COM.elf \

MAP_FILES += \
M5748_COM.map \

SIZE_OUTPUT += \
default.size.stdout \

OBJDUMP_LIST += \
M5748_COM.list \


# All Target
all: main-build

# Main-build Target
main-build: M5748_COM.elf secondary-outputs

# Tool invocations
M5748_COM.elf M5748_COM.map: $(OBJS) $(USER_OBJS) C:\Projects\M5748\M5748_COM\CUBE\ IDE\STM32F103CBTX_FLASH.ld makefile objects.list $(OPTIONAL_TOOL_DEPS)
	arm-none-eabi-gcc -o "M5748_COM.elf" @"objects.list" $(USER_OBJS) $(LIBS) -mcpu=cortex-m3 -T"C:\Projects\M5748\M5748_COM\CUBE IDE\STM32F103CBTX_FLASH.ld" --specs=nosys.specs -Wl,-Map="M5748_COM.map" -Wl,--gc-sections -static --specs=nano.specs -mfloat-abi=soft -mthumb -Wl,--start-group -lc -lm -Wl,--end-group
	@echo 'Finished building target: $@'
	@echo ' '

default.size.stdout: $(EXECUTABLES) makefile objects.list $(OPTIONAL_TOOL_DEPS)
	arm-none-eabi-size  $(EXECUTABLES)
	@echo 'Finished building: $@'
	@echo ' '

M5748_COM.list: $(EXECUTABLES) makefile objects.list $(OPTIONAL_TOOL_DEPS)
	arm-none-eabi-objdump -h -S $(EXECUTABLES) > "M5748_COM.list"
	@echo 'Finished building: $@'
	@echo ' '

# Other Targets
clean:
	-$(RM) M5748_COM.elf M5748_COM.list M5748_COM.map default.size.stdout
	-@echo ' '

secondary-outputs: $(SIZE_OUTPUT) $(OBJDUMP_LIST)

fail-specified-linker-script-missing:
	@echo 'Error: Cannot find the specified linker script. Check the linker settings in the build configuration.'
	@exit 2

warn-no-linker-script-specified:
	@echo 'Warning: No linker script specified. Check the linker settings in the build configuration.'

.PHONY: all clean dependents main-build fail-specified-linker-script-missing warn-no-linker-script-specified

-include ../makefile.targets
