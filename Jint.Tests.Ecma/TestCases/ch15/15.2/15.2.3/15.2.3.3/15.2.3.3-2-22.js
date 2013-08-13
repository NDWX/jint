/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.2/15.2.3/15.2.3.3/15.2.3.3-2-22.js
 * @description Object.getOwnPropertyDescriptor - argument 'P' is a number that converts to a string (value is 0.000001)
 */


function testcase() {
        var obj = { "0.000001": 1 };

        var desc = Object.getOwnPropertyDescriptor(obj, 0.000001);

        return desc.value === 1;
    }
runTestCase(testcase);