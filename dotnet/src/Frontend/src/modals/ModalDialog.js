import React from 'react';
import SkyLight from 'react-skylight';

export default class ModalDialog extends React.Component {
  constructor(props) {
      super(props);
  }

  componentDidUpdate() {
      if (this.state && this.state.content) {
        this.self.show();
      }
  }

  renderInner() {
    if (this.state == null || this.state.content == null) {
        return null;
    }
    else {
        return React.createElement(this.state.content, {
          onSubmit: values => {
            this.self.hide();
            if (this.state && typeof this.state.onSubmit == "function") {
              this.state.content = null;
              this.state.onSubmit(values);
            }
          }
        });
    }
  }

  render() {
    return (
      <SkyLight dialogStyles={{height: "inherit"}} ref={el => this.self=(el||this.self)} >
        {this.renderInner()}
      </SkyLight>
    );
  }
}