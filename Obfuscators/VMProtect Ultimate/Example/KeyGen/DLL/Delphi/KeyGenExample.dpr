program KeyGenExample;

{$APPTYPE CONSOLE}

uses
  SysUtils;

const
  // VMProtectErrors
  ALL_RIGHT = 0;
  UNSUPPORTED_ALGORITHM = 1;
  UNSUPPORTED_NUMBER_OF_BITS = 2;
  USER_NAME_IS_TOO_LONG = 3;
  EMAIL_IS_TOO_LONG = 4;
  USER_DATA_IS_TOO_LONG = 5;
  HWID_HAS_BAD_SIZE = 6;
  PRODUCT_CODE_HAS_BAD_SIZE = 7;
  SERIAL_NUMBER_TOO_LONG = 8;
  BAD_PRODUCT_INFO = 9;
  BAD_SERIAL_NUMBER_INFO = 10;
  BAD_SERIAL_NUMBER_CONTAINER = 11;
  NOT_EMPTY_SERIAL_NUMBER_CONTAINER = 12;
  BAD_PRIVATE_EXPONENT = 13;
  BAD_MODULUS = 14;

  // VMProtectSerialNumberFlags
  HAS_USER_NAME = $00000001;
  HAS_EMAIL = $00000002;
  HAS_EXP_DATE = $00000004;
  HAS_MAX_BUILD_DATE = $00000008;
  HAS_TIME_LIMIT = $00000010;
  HAS_HARDWARE_ID = $00000020;
  HAS_USER_DATA = $00000040;
  SN_FLAGS_PADDING = $FFFFFFFF;

  // VMProtectAlgorithms
  ALGORITHM_RSA = 0;
  ALGORITHM_PADDING = $FFFFFFFF;   

type
  PVMProtectProductInfo = ^VMProtectProductInfo;
  VMProtectProductInfo = packed record
    algorithm: Longword;
    nBits: Longword;
    nPrivateSize: Longword;
    pPrivate: PByte;
    nModulusSize: Longword;
    pModulus: PByte;
    nProductCodeSize: Longword;
    pProductCode: PByte;
  end;

  PVMProtectSerialNumberInfo = ^VMProtectSerialNumberInfo;
  VMProtectSerialNumberInfo = packed record
    flags: Integer;
    pUserName: PWideChar;
    pEMail: PWideChar;
    dwExpDate: Longword;
    dwMaxBuildDate: Longword;
    nRunningTimeLimit: Byte;
    pHardwareID: PAnsiChar;
    nUserDataLength: Longword;
    pUserData: PByte;
  end;

const
  KeyGenDLLName = 'KeyGen32.dll';

function VMProtectGenerateSerialNumber(pProductInfo: PVMProtectProductInfo; pSerialInfo: PVMProtectSerialNumberInfo; pSerialNumber: Pointer): Longword; stdcall; external KeyGenDLLName;
procedure VMProtectFreeSerialNumberMemory(pSerialNumber: Pointer); stdcall; external KeyGenDLLName;

const
//////////////////////////////////////////////////////////////////////////
// !!! this block should be generated by VMProtect License Manager !!! ///
//////////////////////////////////////////////////////////////////////////
g_Algorithm: Longword = ALGORITHM_RSA;
g_nBits: Longword = 0;
g_vModulus: array [0..0] of Byte = (0);
g_vPrivate: array [0..0] of Byte = (0);
g_vProductCode: array [0..0] of Byte = (0);
//////////////////////////////////////////////////////////////////////////
//////////////////////////////////////////////////////////////////////////

var pi: VMProtectProductInfo;
    si: VMProtectSerialNumberInfo;
    res: Longword;
    pBuf: PAnsiChar;
begin
  pi.algorithm := g_Algorithm;
  pi.nBits := g_nBits;
  pi.nModulusSize := sizeof(g_vModulus);
  pi.pModulus := @g_vModulus;
  pi.nPrivateSize := sizeof(g_vPrivate);
  pi.pPrivate := @g_vPrivate;
  pi.nProductCodeSize := sizeof(g_vProductCode);
  pi.pProductCode := @g_vProductCode;

  FillChar(si, sizeof(si), 0);
  si.flags := HAS_USER_NAME or HAS_EMAIL;
  si.pUserName := 'John Doe';
  si.pEMail := 'john@doe.com';
  pBuf := nil;
  res := VMProtectGenerateSerialNumber(@pi, @si, @pBuf);
  if (res = ALL_RIGHT) then begin
    Writeln(Format('Serial number:'#10'%s', [pBuf]));
    VMProtectFreeSerialNumberMemory(pBuf);
  end
  else begin
    Writeln(Format('Error: %d', [res]));
  end;
end.
