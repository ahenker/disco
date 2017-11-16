import React, { Component } from 'react'

import Select from 'react-select';
// Be sure to include styles at some point, probably during your bootstrapping
import 'react-select/dist/react-select.css';
import { SketchPicker, TwitterPicker, AlphaPicker, BlockPicker,
ChromePicker, CirclePicker, CompactPicker, GithubPicker, 
HuePicker, MaterialPicker, PhotoshopPicker, SliderPicker,
SwatchesPicker } from 'react-color';

// This is a simple example to show how to create a custom widget for Iris
// in JS. We just define a simple React component that draws a square with
// black or transparent background depending on the value of a pin.

// Note the code uses several helpers, like `findPinByName`. These are
// available to JS in the `IrisLib` global variable. The available methods
// can be seen in the Main.fs file of the Frontend.fsproj project. Other
// helpers can also be requested.



class TestWidget extends React.Component {
  constructor(props) {
    super(props);
    //initialize 
    this.state={
      groupName: "",
      pinName: "",
      groupPin: "",
      pinVal: "",
      inputVal: "",
      pin: null,
      value: {value:HuePicker, label: "HuePicker"},
      color:"#fff",
      options : [
          {value: HuePicker, label: "HuePicker"},
          {value:SketchPicker, label: "SketchPicker"},
          {value:TwitterPicker, label: "TwitterPicker"},
          {value:AlphaPicker, label: "AlphaPicker"},
          {value:BlockPicker, label: "BlockPicker"},
          {value:ChromePicker, label: "ChromePicker"},
          {value:CirclePicker, label: "CirclePicker"},
          {value:CompactPicker, label: "CompactPicker"},
          {value:GithubPicker, label: "GithubPicker"},
          {value:MaterialPicker, label: "MaterialPicker"},
          {value:PhotoshopPicker, label: "PhotoshopPicker"},
          {value:SliderPicker, label: "SliderPicker"},
          {value:SwatchesPicker, label: "SwatchesPicker"},
      ]
      }
  }


  //check if property "inputVal" exists and set as new pinVal?
  setPinVal() {
    console.log("trying to set pinval");
    
    if (this.state.pin) {  
      IrisLib.updatePinValueAt(this.state.pin, 0, this.state.color)    
    }
  }


  makeCallback(propName) {
    return (ev) => {
      var state = {}
      state[propName] = ev.target.value
      this.setState(state)
    }
  }

  setPin() {
    let groupPin = this.state.groupName + '/'+ this.state.pinName
    //set pin to this states current pin by pinName
    var pin = IrisLib.findPinByName(this.props.model, groupPin);
    this.setState({ 
      groupPin: groupPin,
      pin: pin,
      pinVal: pin ? IrisLib.getPinValueAt(pin, 0) : ""
    }, () => {
      console.log('pin has been changed: ', this.state.groupPin)
    })
  }


  //hadnles button click
  click(event){
    this.setPin();
  }

  logChange(val) {
    console.log('Selected: ', val);
    this.setState({
      value : val,
    });
  }

  handleChangeComplete = (color) => {
    this.setState({ color: color.hex });
    console.log(this.state.color)
    this.setPinVal();
    console.log("this.state.value", this.state.value)
    console.log("this.state.value.value",  + this.state.value.value)
    
  };

  render() {
    return (
      <div style={{
        display: "flex",
        flexDirection: "column",
        justifyContent: "center",
        height: "100%"
      }}>
      <div>
        {/*input to select a pins group*/}
        <label>
          group name
          {/*onChange updates state with new groupName as read from input field*/}
          <input type="text" onChange={this.makeCallback("groupName")} />
        </label>
        {/*input to select a pins name*/}
        <label>
          pin name
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" onChange={this.makeCallback("pinName")} />
        </label>
        <label>
          value
          {/*onChange updates state with new pinName as read from input field*/}
          <input type="text" disabled={this.state.pin == null} onChange={(event) => {
            this.setState({
              inputVal: event.target.value
            }, this.setPinVal.bind(this));
          }} />
        </label>
          {/*after pressing submit button this.state.groupName is updated to hold the full pin name*/}
        <button type="submit" onClick={this.click.bind(this)}>submit</button>
      </div>
        <div style={{margin: "0 10px"}}>
        {/*onChhange updates the state with new slider maximum value*/}
        <label>select</label>
          <Select
          name="form-field-name"
          value={this.state.value.label}
          options={this.state.options}
          onChange={this.logChange.bind(this)}
          />

          <label>colorPicker</label>
          <this.state.value.value
          color={this.state.color}
          onChangeComplete={this.handleChangeComplete} />
        </div>
      </div>
    )
  }
}



// The widget scripts must export a function that receives an id
// and returns an object with the following properties, this may
// change a little bit to make the API more usable from JS.
export default function createWidget (id, name) {
  return {
    Id: id ,
    Name: name,
    InitialLayout: {
      i: id, static: false,
      x: 0, y: 0, w: 3, h: 3,
      minW: 2, minH: 2
    },
    // The Render method receives a dispatch function to send messages
    // to the global state and a model represing the current snapshot of
    // the state. The Render method must return a React element.
    Render(dispatch, model) {
      // Here we use the `renderWidget` which accepts a function to
      // render the body and optionally another to render a header in
      // the title bar of the widget.
      var body = function (dispatch, model) {
        return <TestWidget groupName="foo" pinName="VVVV/design.4vp/Z" pinVal="ho"  model={model} />
      }
      return IrisLib.renderWidget(id, name, null, body, dispatch, model);
    }
  }
}
