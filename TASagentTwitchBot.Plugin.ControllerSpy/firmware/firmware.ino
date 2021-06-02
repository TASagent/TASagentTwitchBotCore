///////////////////////////////////////////////////////////////////////////////
// RetroSpy Firmware for Arduino Uno & Teensy 3.5
// v4.2.3
// RetroSpy written by zoggins of RetroSpy Technologies
// NintendoSpy originally written by jaburns

// NOTE: If you are having input display lag problems try uncommenting out this line.
// #define LAG_FIX
// WARNING!!! If you do uncomment out this line you must enable "Options -> Use Lag Fix" in the UI

// ---------- Uncomment one of these options to select operation mode ---------
// 
//#define MODE_NES
#define MODE_SNES

// Some consoles care about PAL/NTSC for timing purposes
#define VIDEO_OUTPUT VIDEO_NTSC

///////////////////////////////////////////////////////////////////////////////
// ---------- NOTHING BELOW THIS LINE SHOULD BE MODIFIED  -------------------//
///////////////////////////////////////////////////////////////////////////////

#include "common.h"

#include "SNES.h"

#if defined(MODE_NES)
NESSpy NESSpy;
#endif
#if defined(MODE_POWERGLOVE)
PowerGloveSpy PowerGloveSpy;
#endif
#if defined(MODE_SNES)
SNESSpy SNESSpy;
#endif
#if defined(MODE_N64)
N64Spy N64Spy;
#endif
#if defined(MODE_GC)
GCSpy GCSpy;
#endif
#if defined(MODE_BOOSTER_GRIP)
BoosterGripSpy BoosterGripSpy;
#endif
#if defined(MODE_GENESIS)
GenesisSpy GenesisSpy;
#endif
#if defined(MODE_GENESIS_MOUSE)
GenesisMouseSpy GenesisMouseSpy;
#endif
#if defined(MODE_SMS)
SMSSpy SMSSpy;
#endif
#if defined(MODE_SMS_PADDLE)
SMSPaddleSpy SMSPaddleSpy;
#endif
#if defined(MODE_SMS_SPORTS_PAD)
SMSSportsPadSpy SMSSportsPadSpy;
#endif
#if defined(MODE_SMS_ON_GENESIS)
SMSSpy SMSOnGenesisSpy;
#endif
#if defined(MODE_SATURN)
SaturnSpy SaturnSpy;
#endif
#if defined(MODE_SATURN3D)
Saturn3DSpy Saturn3DSpy;
#endif
#if defined(MODE_COLECOVISION)
ColecoVisionSpy ColecoVisionSpy;
#endif
#if defined(MODE_FMTOWNS)
FMTownsSpy FMTownsSpy;
#endif
#if defined(MODE_INTELLIVISION)
IntellivisionSpy IntelliVisionSpy;
#endif
#if defined(MODE_JAGUAR)
JaguarSpy JaguarSpy;
#endif
#if defined(MODE_NEOGEO)
NeoGeoSpy NeoGeoSpy;
#endif
#if defined(MODE_PCFX)
PCFXSpy PCFXSpy;
#endif
#if  defined(MODE_PLAYSTATION)
PlayStationSpy PlayStationSpy;
#endif
#if defined(MODE_TG16)
TG16Spy TG16Spy;
#endif
#if defined(MODE_3DO)
ThreeDOSpy ThreeDOSpy;
#endif
#if defined(MODE_DREAMCAST)
DreamcastSpy DCSpy;
#endif
#if defined(MODE_WII)
WiiSpy WiiSpy;
#endif
#if defined(MODE_CD32)
AmigaCd32Spy Cd32Spy;
#endif
#if defined(MODE_DRIVING_CONTROLLER)
DrivingControllerSpy DrivingControllerSpy;
#endif
#if defined(MODE_PIPPIN)
PippinSpy PippinSpy;
#endif
#if defined(MODE_AMIGA_KEYBOARD)
AmigaKeyboardSpy AmigaKeyboardSpy;
#endif
#if defined(MODE_AMIGA_MOUSE)                                            
AmigaMouseSpy AmigaMouseSpy;
#endif
#if defined(MODE_CDTV_WIRED)
CDTVWiredSpy CDTVWiredSpy;
#endif
#if defined(MODE_CDTV_WIRELESS)
CDTVWirelessSpy CDTVWirelessSpy;
#endif
#if defined(MODE_FMTOWNS_KEYBOARD_AND_MOUSE)
FMTownsKeyboardAndMouseSpy FMTownsKeyboardAndMouseSpy;
#endif
#if defined(MODE_CDI)
CDiSpy CDiSpy(CDI_WIRED_TIMEOUT, CDI_WIRELESS_TIMEOUT);
#endif
#if defined(MODE_CDI_KEYBOARD)
CDiKeyboardSpy CDiKeyboardSpy;
#endif
#if defined(MODE_GAMEBOY_PRINTER)
GameBoyPrinterEmulator GameBoyPrinterEmulator;
#endif
#if defined(MODE_AMIGA_ANALOG_1) || defined(MODE_AMIGA_ANALOG_2)
AmigaAnalogSpy AmigaAnalogSpy;
#endif
#if defined(MODE_ATARI5200_1) || defined(MODE_ATARI5200_2)
Atari5200Spy Atari5200Spy;
#endif
#if defined(MODE_COLECOVISION_ROLLER)                                                  
ColecoVisionRollerSpy ColecoVisionRollerSpy;
#endif
#if defined(MODE_ATARI_PADDLES)                                                  
AtariPaddlesSpy AtariPaddlesSpy;
#endif
#if defined(MODE_KEYBOARD_CONTROLLER) \
	|| defined(MODE_KEYBOARD_CONTROLLER_STAR_RAIDERS) \
	|| defined(MODE_KEYBOARD_CONTROLLER_BIG_BIRD)
KeyboardControllerSpy KeyboardControllerSpy;
#endif

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// General initialization, just sets all pins to input and starts serial communication.
void setup()
{
  // for MODE_DETECT
#if defined(__arm__) && defined(CORE_TEENSY)
  for(int i = 33; i < 40; ++i)
    pinMode(i, INPUT_PULLUP);
#else
#if !defined(MODE_ATARI_PADDLES) && !defined(MODE_ATARI5200_1) && !defined(MODE_ATARI5200_2) && !defined(MODE_AMIGA_ANALOG_1) && !defined(MODE_AMIGA_ANALOG_2)
    PORTC = 0xFF; // Set the pull-ups on the port we use to check operation mode.
    DDRC  = 0x00;
#endif
#endif

#ifdef LAG_FIX
	Serial.begin(57600);
#else
	Serial.begin(115200);
#endif

#if defined(MODE_DETECT)
    if ( !PINC_READ(MODEPIN_SNES)) {
	    delay(1000);
		Serial.println("Starting up in SNES mode");
	    delay(1000);
        SNESSpy.setup();
    } else if ( !PINC_READ(MODEPIN_N64))  {
	    delay(1000);
	    Serial.println("Starting up in N64 mode");
	    delay(1000);
        N64Spy.setup();
    } else if ( !PINC_READ(MODEPIN_GC)) {
	    delay(1000);
	    Serial.println("Starting up in Gamecube mode");
	    delay(1000);
        GCSpy.setup();
    }
#if defined(__arm__) && defined(CORE_TEENSY)
  else if( !PINC_READ( MODEPIN_DREAMCAST ) ) {
	  delay(1000);
	  Serial.println("Starting up in Dreamcast mode");
	  delay(1000);
       DCSpy.setup();
    } else if( !PINC_READ( MODEPIN_WII ) ) {
	    delay(1000);
	    Serial.println("Starting up in Wii mode");
	    delay(1000);
        WiiSpy.setup();
    }
#endif 
    else {
	    delay(1000);
	    Serial.println("Starting up in NES mode");
	    delay(1000);
        NESSpy.setup();
    }
#elif defined(MODE_NES)
    NESSpy.setup();
#elif defined(MODE_POWERGLOVE)
	PowerGloveSpy.setup();
#elif defined(MODE_SNES)
    SNESSpy.setup();
#elif defined(MODE_N64)
    N64Spy.setup();
#elif defined(MODE_GC)
    GCSpy.setup();
#elif defined(MODE_BOOSTER_GRIP)
    BoosterGripSpy.setup();
#elif defined(MODE_GENESIS)
    GenesisSpy.setup();
#elif defined(MODE_GENESIS_MOUSE)
    GenesisMouseSpy.setup();
#elif defined(MODE_SMS)
    SMSSpy.setup();
#elif defined(MODE_SMS_PADDLE)
	SMSPaddleSpy.setup();
#elif defined(MODE_SMS_SPORTS_PAD)
	SMSSportsPadSpy.setup();
#elif defined(MODE_SMS_ON_GENESIS)
    SMSOnGenesisSpy.setup(SMSSpy::OUTPUT_GENESIS);
#elif defined(MODE_SATURN)
    SaturnSpy.setup();
#elif defined(MODE_SATURN3D)
    Saturn3DSpy.setup();
#elif defined(MODE_COLECOVISION)
    ColecoVisionSpy.setup();
#elif defined(MODE_FMTOWNS)
    FMTownsSpy.setup();
#elif defined(MODE_INTELLIVISION)
    IntelliVisionSpy.setup();
#elif defined(MODE_JAGUAR)
    JaguarSpy.setup();
#elif defined(MODE_NEOGEO)
    NeoGeoSpy.setup();
#elif defined(MODE_PCFX)
    PCFXSpy.setup();
#elif defined(MODE_PLAYSTATION)
    PlayStationSpy.setup();
#elif defined(MODE_TG16)
    TG16Spy.setup();
#elif defined(MODE_3DO)
    ThreeDOSpy.setup();
#elif defined(MODE_DREAMCAST)
    DCSpy.setup();
#elif defined(MODE_WII)
    WiiSpy.setup();
#elif defined(MODE_CD32)
    Cd32Spy.setup();    
#elif defined(MODE_DRIVING_CONTROLLER)
	DrivingControllerSpy.setup();
#elif defined(MODE_PIPPIN)
	PippinSpy.setup(PIPPIN_CONTROLLER_SPY_ADDRESS, PIPPIN_MOUSE_SPY_ADDRESS);
#elif defined(MODE_AMIGA_KEYBOARD)
	AmigaKeyboardSpy.setup();
#elif defined(MODE_AMIGA_MOUSE)
	AmigaMouseSpy.setup(VIDEO_OUTPUT);
#elif defined(MODE_CDTV_WIRED)
	CDTVWiredSpy.setup();
#elif defined(MODE_CDTV_WIRELESS)
	CDTVWirelessSpy.setup();
#elif defined(MODE_FMTOWNS_KEYBOARD_AND_MOUSE)
	FMTownsKeyboardAndMouseSpy.setup();
#elif defined(MODE_CDI)
	CDiSpy.setup();
#elif defined(MODE_CDI_KEYBOARD)
	CDiKeyboardSpy.setup();
#elif defined(MODE_GAMEBOY_PRINTER)
	GameBoyPrinterEmulator.setup();
#elif defined(MODE_AMIGA_ANALOG_1)
	AmigaAnalogSpy.setup(false);
#elif defined(MODE_AMIGA_ANALOG_2)
	AmigaAnalogSpy.setup(true);
#elif defined(MODE_ATARI5200_1)
	Atari5200Spy.setup(false);
#elif defined(MODE_ATARI5200_2)
	Atari5200Spy.setup(true);
#elif defined(MODE_KEYBOARD_CONTROLLER)
	KeyboardControllerSpy.setup(KeyboardControllerSpy::MODE_NORMAL);
#elif defined(MODE_KEYBOARD_CONTROLLER_STAR_RAIDERS)
	KeyboardControllerSpy.setup(KeyboardControllerSpy::MODE_STAR_RAIDERS);
#elif defined(MODE_KEYBOARD_CONTROLLER_BIG_BIRD)
	KeyboardControllerSpy.setup(KeyboardControllerSpy::MODE_BIG_BIRD);
#elif defined(MODE_COLECOVISION_ROLLER)
	ColecoVisionRollerSpy.setup(VIDEO_OUTPUT);
#elif defined(MODE_ATARI_PADDLES)
	AtariPaddlesSpy.setup();
#endif

  #pragma GCC diagnostic push
  #pragma GCC diagnostic ignored "-Wunused-value"
  T_DELAY(5000);
  A_DELAY(200);
  #pragma GCC diagnostic pop
  

}

/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
// Arduino sketch main loop definition.
void loop()
{
#if defined(MODE_DETECT)
    if( !PINC_READ( MODEPIN_SNES ) ) {
        SNESSpy.loop();
    } else if( !PINC_READ( MODEPIN_N64 ) ) {
        N64Spy.loop();
    } else if( !PINC_READ( MODEPIN_GC ) ) {
        GCSpy.loop();
    } 
#if defined(__arm__) && defined(CORE_TEENSY)
  else if( !PINC_READ( MODEPIN_DREAMCAST ) ) {
        DCSpy.loop();
    } else if( !PINC_READ( MODEPIN_WII ) ) {
        WiiSpy.loop();
    }
#endif
  else {
        NESSpy.loop();
    }
#elif defined(MODE_GC)
    GCSpy.loop();
#elif defined(MODE_N64)
    N64Spy.loop();
#elif defined(MODE_SNES)
    SNESSpy.loop();
#elif defined(MODE_NES)
    NESSpy.loop();
#elif defined(MODE_POWERGLOVE)
	PowerGloveSpy.loop();
#elif defined(MODE_BOOSTER_GRIP)
    BoosterGripSpy.loop();
#elif defined(MODE_GENESIS)
    GenesisSpy.loop();
#elif defined(MODE_GENESIS_MOUSE)
    GenesisMouseSpy.loop();
#elif defined(MODE_SMS)
    SMSSpy.loop();
#elif defined(MODE_SMS_PADDLE)
	SMSPaddleSpy.loop();
#elif defined(MODE_SMS_SPORTS_PAD)
	SMSSportsPadSpy.loop();
#elif defined(MODE_SMS_ON_GENESIS)
    SMSOnGenesisSpy.loop();
#elif defined(MODE_SATURN)
    SaturnSpy.loop();
#elif defined(MODE_SATURN3D)
    Saturn3DSpy.loop();
#elif defined(MODE_COLECOVISION)
    ColecoVisionSpy.loop();
#elif defined(MODE_FMTOWNS)
    FMTownsSpy.loop();
#elif defined(MODE_INTELLIVISION)
    IntelliVisionSpy.loop();
#elif defined(MODE_JAGUAR)
    JaguarSpy.loop();
#elif defined(MODE_NEOGEO)
    NeoGeoSpy.loop();
#elif defined(MODE_PCFX)
    PCFXSpy.loop();
#elif defined(MODE_PLAYSTATION)
    PlayStationSpy.loop();
#elif defined(MODE_TG16)
    TG16Spy.loop();
#elif defined(MODE_3DO)
    ThreeDOSpy.loop();
#elif defined(MODE_DREAMCAST)
    DCSpy.loop();
#elif defined(MODE_WII)
    WiiSpy.loop();
#elif defined(MODE_CD32)
   Cd32Spy.loop();
#elif defined(MODE_DRIVING_CONTROLLER)
   DrivingControllerSpy.loop();
#elif defined(MODE_PIPPIN)
	PippinSpy.loop();
#elif defined(MODE_AMIGA_KEYBOARD)
	AmigaKeyboardSpy.loop();
#elif defined(MODE_AMIGA_MOUSE)
	AmigaMouseSpy.loop();
#elif defined(MODE_CDTV_WIRED)
	CDTVWiredSpy.loop();
#elif defined(MODE_CDTV_WIRELESS)
	CDTVWirelessSpy.loop();
#elif defined(MODE_FMTOWNS_KEYBOARD_AND_MOUSE)
	FMTownsKeyboardAndMouseSpy.loop();
#elif defined(MODE_CDI)
	CDiSpy.loop();
#elif defined(MODE_CDI_KEYBOARD)
	CDiKeyboardSpy.loop();
#elif defined(MODE_GAMEBOY_PRINTER)
	GameBoyPrinterEmulator.loop();
#elif defined(MODE_AMIGA_ANALOG_1) || defined(MODE_AMIGA_ANALOG_2)
	AmigaAnalogSpy.loop();
#elif defined(MODE_ATARI5200_1) || defined(MODE_ATARI5200_2)
	Atari5200Spy.loop();
#elif defined(MODE_COLECOVISION_ROLLER)
	ColecoVisionRollerSpy.loop();
#elif defined(MODE_ATARI_PADDLES)
	AtariPaddlesSpy.loop();
#elif defined(MODE_KEYBOARD_CONTROLLER) \
	|| defined(MODE_KEYBOARD_CONTROLLER_STAR_RAIDERS) \
	|| defined(MODE_KEYBOARD_CONTROLLER_BIG_BIRD) 
	KeyboardControllerSpy.loop();
#endif

}
