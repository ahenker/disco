import React, { Component } from 'react'

export default class CreateProject extends Component {
  constructor(props) {
      super(props);
      let match = /\:(\d+)$/.exec(window.location.host);
      this.webPort = match != null ? match[1] : null;
      this.state = {
        name: "",
        nameError: "Required"
      };
  }

  validateName(id, name) {
    return {
      value: name,
      error: typeof name === "string" && name.length > 0 ? null : "Required"
    };
  }

  validateIpAddress(id, address) {
    return {
      value: address,
      error: /^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$/.test(address) ? null : "Not a valid IP address"
    };
  }

  validatePort(id, port) {
    // TODO: Check also range?
    const parsed = parseInt(port);
    if (isNaN(parsed)) {
      return { value: port, error: "Not a valid number" };
    }
    if (this.webPort && parsed === this.webPort) {
      return { value: parsed, error: "Port in use by web server" };
    }
    for (let key in this.state) {
      if (key !== id && key.toLowerCase().endsWith("port")) {
        if (this.state[key] === parsed) {
          return { value: parsed, error: "Duplicated port" };
        }
      }
    }
    return { value: parsed, error: null };;
  }

  renderGroup(id, label, validate, placeholder = "") {
    const success = this.state[id + "Error"] == null;
    return (
      <div className="field">
        <label className="label">{label}</label>
        <p className="control has-icon has-icon-right">
          <input className={"input " + (success ? "is-success" : "is-danger")}
            type="text" placeholder={placeholder} value={this.state[id]} onChange={ev => {
              const value = ev.target.value;
              const validation = validate(id, value);
              this.setState({ [id]: validation.value, [id + "Error"]: validation.error})
            }}/>
          <span className="icon is-small">
            <i className={"fa " + (success ? "fa-check" : "fa-warning")}></i>
          </span>
        </p>
        {!success && <p className="help is-danger">{this.state[id + "Error"]}</p>}
      </div>
    );
  }

  render() {
    let isValid = true;
    for (let key in this.state) {
      if (key.endsWith("Error") && this.state[key] != null) {
        isValid = false;
        break;
      }
    }
    return (
      <div>
        <p className="title has-text-centered">Create Project</p>
        {this.renderGroup("name", "Name", this.validateName.bind(this))}
        <div className="field is-grouped">
          <p className="control">
            <button className="button is-primary" disabled={!isValid} onClick={ev => {
              ev.preventDefault();
              this.props.onSubmit(this.state.name);
            }}>
              Submit
            </button>
          </p>
        </div>
      </div>
    );
  }
}
