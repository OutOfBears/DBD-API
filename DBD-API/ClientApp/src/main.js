import Vue from "vue";
import App from "./App";
import router from "./router";
import Antd from 'ant-design-vue';
import VueMoment from 'vue-moment'
import moment from 'moment-timezone'
import 'ant-design-vue/dist/antd.css';

Vue.config.productionTip = false;
Vue.$router = router;
Vue.use(Antd);
Vue.use(VueMoment, { moment });

console.log(Vue.$moment);

new Vue({
  router,
  render: h => h(App),
}).$mount("#app");
