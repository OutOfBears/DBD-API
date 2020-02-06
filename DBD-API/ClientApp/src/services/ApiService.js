const urlBase = process.env.NODE_ENV === "development" ?
  "https://localhost:5001" : `${window.location.protocol}//${window.location.host}`;

const apiBase = `${urlBase}/api`;

const convertBranch = (branch) => {
  switch(branch) {
    case "live":
    case "Public":
      return "Public";
    default:
      return "";
  }
};

const urlEncode = (params) => {
  let data = Object.entries(params);
  data = data.map(([k, v]) => `${encodeURIComponent(k)}=${encodeURIComponent(v)}`);
  return data.join('&');
};

const lazyRequest = (branch, endpoint, extraData = undefined) => {
  branch = encodeURIComponent(branch);

  let url = `${apiBase}/${endpoint}?branch=${branch}`;
  if(typeof extraData === 'object')
    url = `${url}&${urlEncode(extraData)}`;

  return new Promise((res, rej) => {
    fetch(url)
      .then(resp => {
        if(resp.status === 200)
          return resp.json();
        throw "Invalid server response";
      })
      .then(data => {
        return res(data);
      })
      .catch(rej);
  });
};

export default {
  // url conversion
  getIconUrl: function(branch, url) {
    branch = convertBranch(branch);
    return `${urlBase}/data/${branch}/${url}`;
  },

  // conversions
  convertPlayerRole: function(role){
    switch(role) {
      case "EPlayerRole::VE_Slasher":
        return "Killer";
      case "EPlayerRole::VE_Camper":
        return "Survivor";
      default:
        return "";
    }
  },
  convertGender: function(gender) {
    switch(gender) {
      case "EGender::VE_Male":
        return "Male";
      case "EGender::VE_Female":
        return "Female";
      case "EGender::VE_Multiple":
        return "Male/Female";
      case "EGender::VE_NotHuman":
        return "Monster";
      default:
        return "";
    }
  },
  convertKillerHeight: function(height){
    switch(height){
      case "EKillerHeight::Average":
        return "Average";
      case "EKillerHeight::Short":
        return "Short";
      case "EKillerHeight::Tall":
        return "Tall";
      default:
        return "";
    }
  },
  convertCharacterDifficulty: function(difficulty){
    switch(difficulty) {
      case "ECharacterDifficulty::VE_Easy":
        return "Easy";

      case "ECharacterDifficulty::VE_Intermediate":
        return "Intermediate";

      case "ECharacterDifficulty::VE_Hard":
        return "Hard";

      default:
        return "";
    }
  },

  // api calls
  getDbdNews: function(branch = "live") {
    branch = encodeURIComponent(branch);
    return new Promise((res, rej) => {
      lazyRequest(branch,  "news")
        .then(data => {
          data = (data || {}).news || [];
          data = data.sort((a, b) => {
            return b.weight - a.weight;
          });
          return res(data);
        })
        .catch(rej);
    })
  },
  getItem: (id, branch = "live") => lazyRequest(branch, `items/${id}`),
  getItems: (branch = "live") => lazyRequest(branch, "items"),
  getItemAddons: (branch = "live") => lazyRequest(branch, "itemaddons"),
  getCharacters: (branch = "live") => lazyRequest(branch, "characters"),
  getCharacterByIndex: (id, branch = "live") => lazyRequest(branch, `characters/${id}`),
  getCharacterPerks: (id, branch = "live") => lazyRequest(branch, `perks/${id}`),
  getPerks: (branch = "live") => lazyRequest(branch, "perks"),
  getTunables: (branch = "live") => lazyRequest(branch, "tunables"),
  getOfferings: (branch = "live") => lazyRequest(branch, "offerings"),
  getShrine: (branch = "live") => lazyRequest(branch, "shrineofsecrets"),

  getKillerTunables: function(branch, killer) {
    return new Promise((res, rej) => {
      this.getTunables(branch)
        .then(data => {
          let tunables = {
            baseTunables:   (data.baseTunables        || {}).Killer  || {},
            tunableValues:  (data.killerTunables      || {})[killer] || {},
            tunables:       (data.knownTunableValues  || {})[killer] || {},
          };

          res(tunables);
        })
        .catch(rej)
    });
  },

  getSurvivorItems: function(branch) {
    return new Promise((res, rej) => {
      this.getItems(branch)
        .then(data => {
          let items = Object.values(data).filter(x => {
            return x.role === "EPlayerRole::VE_Camper";
          });

          res(items);
        })
        .catch(rej);
    });
  },

  getSurvivorAddons: function(branch) {
    return new Promise((res, rej) => {
      this.getItemAddons(branch)
        .then(data => {
          let items = Object.values(data).filter(x => {
            return x.role === "EPlayerRole::VE_Camper";
          });

          res(items);
        })
        .catch(rej)
    });
  },

  getKillerAddons: function(branch, killerItem) {
    return new Promise((res, rej) => {
      this.getItemAddons(branch)
        .then(data => {
          let items = Object.values(data).filter(x =>
            x.parentItems.indexOf(killerItem) > -1);
          res(items);
        })
        .catch(rej)
    });
  },
}