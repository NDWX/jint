/// Copyright (c) 2012 Ecma International.  All rights reserved. 
/**
 * @path ch15/15.2/15.2.3/15.2.3.7/15.2.3.7-6-a-225.js
 * @description Object.defineProperties - 'O' is an Array, 'P' is an array index property that already exists on 'O' with [[Configurable]] true, the [[Configurable]] field of 'desc' is true (15.4.5.1 step 4.c)
 */


function testcase() {
        var arr = [];

        Object.defineProperty(arr, "0", {
            configurable: true
        });

        try {
            Object.defineProperties(arr, {
                "0": {
                    configurable: true
                }
            });
            return dataPropertyAttributesAreCorrect(arr, "0", undefined, false, false, true);
        } catch (e) {
            return false;
        }
    }
runTestCase(testcase);
