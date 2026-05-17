#ifndef _FLASH_COMMON_H
#define _FLASH_COMMON_H


// *******************************************************************
// Reserved memory boundries
// *******************************************************************

// *******************************************************************
//
//                     !!! ATENTION !!!!
//
// The actual memory map is defined in "STM32F10x_FLASH.icf" file.
//    so most of the constants in this file MAST be suncronized with 
//    the definitions in the .icf file.
// *******************************************************************

#define BLOCK_SIZE (0x400)
#define BOOT_BLOCK_BASE (0x08000000)
// This address marks the end of '.intvec', defined in 'startup_stm32f10x_cl.c'
#define INT_VEC_TOP (0x08000150)
// Address where we keep the local boot loader copy for safe transition.
#define BOOT_COPY_BLOCK (0x08000400)
// Address for CRC data and other stuff.
#define BOOT_CRC_BLOCK (0x08000600)
// This address is the actual start of the boot loader section
#define BOOT_LOADER_BASE      (0x08000800)
#define BOOT_LOADER_TOP       (0x08004000)
#define BOOT_PERSISTENT_TOP   (0x08004000)
#define BOOT_MEMORY_TOP       (0x08020000)

#define BOOT_CRC_BLOCK_SIZE (0x200)
#define BOOT_BLOCK_SIZE (0x200)

#endif //_FLASH_COMMON_H