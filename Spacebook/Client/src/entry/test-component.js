import { defineCustomElement } from "vue";
import Component from "../components/test-component.vue";
window.customElements.define('test-component', defineCustomElement(Component));