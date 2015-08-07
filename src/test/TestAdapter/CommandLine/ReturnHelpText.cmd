:: Copyright (c) Microsoft. All rights reserved.
:: Licensed under the MIT license. See LICENSE file in the project root for full license information.

REM PTF invokes a cmd script using the following parameters:
REM %1%	 [PtfAdReturn:<type of returnValue>;][<name of outParam1>:<type of outParam1>[;<name of outParam2>:<type of outParam2>]…]
REM %2%	 [<name of inParam1>:<type of inParam1>[;<name of inParam2>:<type of inParam2]
REM %3%  Help text of the method.
REM %4%	 First input parameter.
REM %5%	 Second input parameter.

echo PtfAdReturn=%3
exit 0


