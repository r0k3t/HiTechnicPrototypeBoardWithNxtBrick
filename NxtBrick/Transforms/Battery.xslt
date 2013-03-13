<?xml version="1.0" encoding="UTF-8" ?>
<!-- 
    This file is part of Microsoft Robotics Developer Studio Code Samples.
    Copyright (C) Microsoft Corporation.  All rights reserved.
    $File: Battery.xslt $ $Revision: 5 $
-->
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:b="http://schemas.microsoft.com/robotics/2007/07/lego/nxt/battery.html">

  <xsl:import href="/resources/dss/Microsoft.Dss.Runtime.Home.MasterPage.xslt" />

  <xsl:template match="/">
    <xsl:call-template name="MasterPage">
      <xsl:with-param name="title">

      </xsl:with-param>
      <xsl:with-param name="serviceName">
        NXT Battery Service
      </xsl:with-param>
      <xsl:with-param name="description">
        The current NXT Brick Battery voltage levels
      </xsl:with-param>
      <xsl:with-param name="head">
        <script language="javascript" type="text/javascript" src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Transforms.Common.js"/>
        <script language="javascript" type="text/javascript">
          <xsl:text>
var pollingFrequency = 1000;
var critical = </xsl:text>
          <xsl:value-of select="/b:BatteryState/b:CriticalBatteryVoltage" />
          <xsl:text>;
var maximum = </xsl:text>
          <xsl:value-of select="/b:BatteryState/b:MaxVoltage" />
          <xsl:text>;
var minimum = </xsl:text>
          <xsl:value-of select="/b:BatteryState/b:MinVoltage" />
          <xsl:text>;
var timeStamp;
</xsl:text>

          <![CDATA[

function onState(state)
{
    if (state.TimeStamp != timeStamp)
    {
      timeStamp = state.TimeStamp;
      document.all("voltageCell").innerText = state.CurrentBatteryVoltage + "v";
      
      var current = parseFloat(state.CurrentBatteryVoltage);
      var level = document.all("levelCell");
      var meter = document.all("remainCell");
      
      var remain;
      
      if (current == 0)
      {
        remain = 100;
        level.innerText = "Battery level is not available";
      }
      else if (current < minimum)
      {
        remain = 0;
        level.innerText = "Battery level is critically low";
      }
      else
      {
        remain = parseInt(200 * (current - minimum) / (maximum - minimum));
        level.innerText = "Battery Level: " + parseInt(remain / 2) + "%";
      }
      
      var color;
      if (current > critical)
      {
        color = "#44A";
      }
      else
      {
        color = "#A22";
      }
      
      
      meter.style.backgroundColor = color;
      meter.style.width = remain + "px";
      level.style.color = color;
      
      
    }
    readState(pollingFrequency);
}

function startPolling()
{
  setStateCompletion("onState");
  readState(pollingFrequency);
}

setTimeout("startPolling()", 1000);

          ]]>
        </script>
      </xsl:with-param>
    </xsl:call-template>
  </xsl:template>

  <!--<xsl:output method="html"/>-->

  <xsl:template match="/b:BatteryState">
    <img src="/resources/NxtBrick.Y2007.M07/Microsoft.Robotics.Services.Sample.Lego.Nxt.Resources.NxtBattery.Image.png"
         align="right" alt="Buttons" width="32px" height="32px"/>
    <table>
      <tr class="odd" title="The current voltage being supplied by the batteries in the NXT Brick">
        <th>Current Voltage:</th>
        <td id="voltageCell">
          <xsl:value-of select="b:CurrentBatteryVoltage"/>
          <xsl:text>v</xsl:text>
        </td>
      </tr>
      <tr class="even" title="The maximum permitted voltage of the batteries in the NXT Brick">
        <th >Maximum Voltage:</th>
        <td>
          <xsl:value-of select="b:MaxVoltage"/>
          <xsl:text>v</xsl:text>
        </td>
      </tr>
      <tr class="odd" title="The critical voltage of the batteries in the NXT Brick. When the current voltage falls this low the brick may become unreliable.">
        <th>Critical Voltage:</th>
        <td>
          <xsl:value-of select="b:CriticalBatteryVoltage"/>
          <xsl:text>v</xsl:text>
        </td>
      </tr>
      <tr class="even" title="The minimum permitted voltage of the batteries in the NXT Brick. When the current voltage falls this low the brick cannot function.">
        <th>Minimum Voltage:</th>
        <td>
          <xsl:value-of select="b:MinVoltage"/>
          <xsl:text>v</xsl:text>          
        </td>
      </tr>
      <tr>
        <td colspan="2">
          <xsl:variable name="remain">
            <xsl:choose>
              <xsl:when test="b:CurrentBatteryVoltage = 0">
                <xsl:text>100</xsl:text>
              </xsl:when>
              <xsl:when test="b:CurrentBatteryVoltage &lt; b:MinVoltage">
                <xsl:text>0</xsl:text>
              </xsl:when>
              <xsl:when test="b:CurrentBatteryVoltage &gt; b:MaxVoltage">
                <xsl:text>200</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:value-of select="200 * (b:CurrentBatteryVoltage - b:MinVoltage) div (b:MaxVoltage - b:MinVoltage)"/>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <xsl:variable name="color">
            <xsl:choose>
              <xsl:when test="b:CurrentBatteryVoltage > b:CriticalBatteryVoltage">
                <xsl:text>#44A</xsl:text>
              </xsl:when>
              <xsl:otherwise>
                <xsl:text>#A22</xsl:text>
              </xsl:otherwise>
            </xsl:choose>
          </xsl:variable>
          <table width="200" cellspacing="0" cellpadding="0">
            <tr>
              <td id="levelCell"  colspan="3" style="text-align:center;color:{$color};">
                <xsl:choose>
                  <xsl:when test="b:CurrentBatteryVoltage = 0">
                    <xsl:text>Battery level is not available</xsl:text>
                  </xsl:when>
                  <xsl:otherwise>
                    <xsl:text>Battery Level: </xsl:text>
                    <xsl:value-of select="format-number($remain div 200,'#%')"/>
                  </xsl:otherwise>
                </xsl:choose>
              </td>
            </tr>
            <tr>
              <td colspan="3">
                <div style="position:relative;width:205px;height:60px">
                  <div style="position:absolute;background:#AAF;width:200px;height:100%;left:0px;top:0px"/>
                  <div id="remainCell" style="position:absolute;background-color:{$color};width:{$remain}px;height:60px;left:0px;top:0px"/>
                  <div style="position:absolute;background:#AAF;width:5px;height:33%;left:200px;top:33%;"/>
                  <div style="position:absolute;background:transparent;left:0px;top:0px;width:100%;height:50%;filter:progid:DXImageTransform.Microsoft.Gradient(GradientType=0,StartColorStr='#60FFFFFF',EndColorStr='#20FFFFFF')"/>
                </div>
              </td>
            </tr>
          </table>
        </td>
      </tr>
    </table>
  </xsl:template>
  
</xsl:stylesheet>