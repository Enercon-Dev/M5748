#ifndef _SPI_DISPLAY_H_
#define _SPI_DISPLAY_H_

void spi_Send(uint8_t txBuffer[], uint32_t byfferSize);
void spi_generateStrobe();

#endif //_SPI_DISPLAY_H_