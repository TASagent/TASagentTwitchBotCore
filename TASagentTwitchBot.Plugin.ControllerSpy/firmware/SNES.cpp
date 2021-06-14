//
// SNES.cpp
//
// Author:
//       Christopher "Zoggins" Mallery <zoggins@retro-spy.com>
//
// Copyright (c) 2020 RetroSpy Technologies
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#include "SNES.h"

void SNESSpy::loop() {
	noInterrupts();
	updateState();
	interrupts();
#if !defined(DEBUG)
	writeSerial();
#else
	debugSerial();
#endif
	T_DELAY(5);
}

void SNESSpy::writeSerial() {
	sendRawData(rawData, 0, bytesToReturn);
}

void SNESSpy::debugSerial() {
	sendRawDataDebug(rawData, 0, bytesToReturn);
}

void SNESSpy::updateState() {
#ifdef MODE_2WIRE_SNES
	read_shiftRegister_2wire(rawData, SNES_LATCH, SNES_DATA, false, SNES_BITCOUNT);
#else
	unsigned char position = 0;
	unsigned char bits = 0;

	bytesToReturn = SNES_BITCOUNT;

	WAIT_FALLING_EDGE(SNES_LATCH);

	do {
		WAIT_FALLING_EDGE(SNES_CLOCK);
		rawData[position++] = !PIN_READ(SNES_DATA);
	} while (++bits < SNES_BITCOUNT);

	if (rawData[15] != 0x0 || (rawData[15] == 0x00 && rawData[13] != 0x00))
	{
		bits = 0;
		do {
			WAIT_FALLING_EDGE(SNES_CLOCK);
			rawData[position++] = !PIN_READ(SNES_DATA);
		} while (++bits < SNES_BITCOUNT);

		bytesToReturn = SNES_BITCOUNT_EXT;
	}
#endif
}
