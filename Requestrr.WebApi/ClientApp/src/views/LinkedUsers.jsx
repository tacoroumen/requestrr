import { useEffect, useState } from "react";
import { useDispatch } from 'react-redux';
import { getLinkedUsers, saveLinkedUsers, getOverseerrUsers } from "../store/actions/LinkedUsersActions";

// reactstrap components
import {
  Alert,
  Card,
  CardHeader,
  CardBody,
  FormGroup,
  Form,
  Input,
  Container,
  Row,
  Col
} from "reactstrap";
// core components
import UserHeader from "../components/Headers/UserHeader.jsx";


function LinkedUsers() {
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [saveAttempted, setSaveAttempted] = useState(false);
  const [saveSuccess, setSaveSuccess] = useState(false);
  const [saveError, setSaveError] = useState("");
  const [loadError, setLoadError] = useState("");
  const [userLinks, setUserLinks] = useState([]);
  const [overseerrUsers, setOverseerrUsers] = useState([]);
  const [overseerrUsersError, setOverseerrUsersError] = useState("");

  const dispatch = useDispatch();


  useEffect(() => {
    dispatch(getLinkedUsers())
      .then(data => {
        setIsLoading(false);

        if (data.ok) {
          setUserLinks(data.userLinks.map(x => ({chatUserId: x.chatUserId || "", overseerrUserId: x.overseerrUserId || ""})));
        } else {
          setLoadError(data.error);
        }
      });

    dispatch(getOverseerrUsers())
      .then(data => {
        if (data.ok) {
          setOverseerrUsers(data.users);
          setOverseerrUsersError("");
        } else {
          setOverseerrUsers([]);
          setOverseerrUsersError(data.error);
        }
      });
  }, []);


  const updateUserLink = (index, field, value) => {
    setUserLinks(userLinks.map((link, i) => i === index ? {...link, [field]: value} : link));
  };


  const onSaving = e => {
    e.preventDefault();

    if (isSaving) {
      return;
    }

    setIsSaving(true);

    dispatch(saveLinkedUsers(userLinks
      .filter(x => x.chatUserId.trim() !== "")
      .map(x => ({chatUserId: x.chatUserId.trim(), overseerrUserId: x.overseerrUserId}))))
      .then(data => {
        setIsSaving(false);
        setSaveAttempted(true);
        setSaveSuccess(data.ok);
        setSaveError(data.ok ? "" : data.error);
      });
  };


  return (
    <>
      <UserHeader title="Linked Users" description="This page is for linking Discord and Matrix users to their Seerr accounts" />
      <Container className="mt--7" fluid>
        <Row>
          <Col className="order-xl-1" xl="12">
            <Card className="bg-secondary shadow">
              <CardHeader className="bg-white border-0">
                <Row className="align-items-center">
                  <Col xs="8">
                    <h3 className="mb-0">Configuration</h3>
                  </Col>
                </Row>
              </CardHeader>
              <CardBody className={isLoading ? "fade" : "fade show"}>
                <Form className="complex">
                  <h6 className="heading-small text-muted mb-4">
                    Chat users linked to Seerr
                  </h6>
                  <div className="pl-lg-4">
                    <p className="text-muted text-sm">
                      Requests from a linked user are made as their Seerr user, so they show up under that person in Seerr and follow their permissions and quota.
                      Enter a Matrix user ID (@name:your-domain) or a Discord user ID (right-click the user in Discord with Developer Mode on, then Copy User ID).
                      Add a row per account to link one Seerr user to both a Discord and a Matrix account.
                      For Discord, this list wins over the Discord ID saved in Seerr. Unlinked users request as the default Seerr user set under Download Clients.
                    </p>
                    {
                      loadError || overseerrUsersError ? (
                        <Alert className="mt-3" color="warning">
                          <strong>{loadError || overseerrUsersError}</strong>
                        </Alert>)
                        : null
                    }
                    {
                      userLinks.map((link, index) => (
                        <Row key={index} className="align-items-center mb-2">
                          <Col lg="5">
                            <Input
                              className="form-control-alternative"
                              placeholder="@name:your-domain or Discord user ID"
                              type="text"
                              value={link.chatUserId}
                              onChange={e => updateUserLink(index, "chatUserId", e.target.value)}
                            />
                          </Col>
                          <Col lg="5">
                            <Input
                              className="form-control-alternative"
                              type="select"
                              value={link.overseerrUserId}
                              onChange={e => updateUserLink(index, "overseerrUserId", e.target.value)}>
                              <option value="">Choose a Seerr user</option>
                              {
                                overseerrUsers.map(user => <option key={user.id} value={user.id}>{user.name}</option>)
                              }
                              {
                                link.overseerrUserId && !overseerrUsers.some(user => user.id === link.overseerrUserId)
                                  ? <option value={link.overseerrUserId}>{`Seerr user ${link.overseerrUserId}`}</option>
                                  : null
                              }
                            </Input>
                          </Col>
                          <Col lg="2">
                            <button className="btn btn-sm btn-outline-danger" type="button"
                                    onClick={() => setUserLinks(userLinks.filter((_, i) => i !== index))}>
                              Remove
                            </button>
                          </Col>
                        </Row>
                      ))
                    }
                    <button className="btn btn-sm btn-outline-primary mt-2" type="button"
                            onClick={() => setUserLinks([...userLinks, {chatUserId: "", overseerrUserId: ""}])}>
                      Link a user
                    </button>
                    <Row className="mt-4">
                      <Col>
                        <FormGroup>
                          {
                            saveAttempted && !isSaving ?
                              !saveSuccess ? (
                                <Alert className="text-center" color="danger">
                                  <strong>{saveError}</strong>
                                </Alert>)
                                : <Alert className="text-center" color="success">
                                  <strong>Linked users updated successfully.</strong>
                                </Alert>
                              : null
                          }
                        </FormGroup>
                        <FormGroup className="text-right">
                          <button className="btn btn-icon btn-3 btn-primary" onClick={onSaving} disabled={isSaving || isLoading || !!loadError} type="button">
                            <span className="btn-inner--icon"><i className="fas fa-save"></i></span>
                            <span className="btn-inner--text">Save Changes</span>
                          </button>
                        </FormGroup>
                      </Col>
                    </Row>
                  </div>
                </Form>
              </CardBody>
            </Card>
          </Col>
        </Row>
      </Container>
    </>
  );
}

export default LinkedUsers;
